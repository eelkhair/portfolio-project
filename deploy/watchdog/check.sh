#!/usr/bin/env bash
# watchdog/check.sh — one tick of the LAN-side auto-recovery loop.
#
# Runs from a systemd timer every 60s inside an LXC on the Proxmox node
# OPPOSITE the prod app host (so a node-wide failure cannot take the watchdog
# with it).
#
# Decision tree (state kept in $STATE_DIR):
#   fail count 1-2  → log, debounce
#   fail count 3    → L1: ssh prod, docker compose restart
#   fail count 5    → L2: Proxmox API, qm reboot <prod-vmid>
#   fail count 8    → L3: ssh dev, poweroff (free cluster RAM), retry L2
#   fail count >=10 → stop escalating, keep logging (DNS failover handles it)
#
# Healthy probe → reset counter, Kuma heartbeat.
#
# Flags:
#   --dry-run         print actions but don't execute
#   FORCE_LEVEL=1..3  override decision and fire that level (for verification)
set -euo pipefail

CONFIG_FILE="${CONFIG_FILE:-/opt/watchdog/config.env}"
STATE_DIR="${STATE_DIR:-/var/lib/watchdog}"
STATE_FILE="${STATE_DIR}/state"
DRY_RUN=0
[[ "${1:-}" == "--dry-run" ]] && DRY_RUN=1

# ── config ────────────────────────────────────────────────────────────────
if [[ ! -r "$CONFIG_FILE" ]]; then
  echo "config not readable: $CONFIG_FILE" >&2
  exit 2
fi
# shellcheck disable=SC1090
source "$CONFIG_FILE"

: "${PROD_HOST:?PROD_HOST must be set}"
: "${PROD_NODE:?PROD_NODE must be set (Proxmox node running prod VM)}"
: "${PROD_VMID:?PROD_VMID must be set}"
: "${DEV_HOST:?DEV_HOST must be set}"
: "${PVE_API_HOST:?PVE_API_HOST must be set (eg eelkhair2.lan:8006)}"
: "${PVE_TOKEN_ID:?PVE_TOKEN_ID must be set}"
: "${PVE_TOKEN_SECRET:?PVE_TOKEN_SECRET must be set}"
# DEV_VMID is OPTIONAL — unset/empty means "don't touch dev". When set, the
# watchdog enforces "dev stays off" by calling qm stop after any prod recovery.
DEV_VMID="${DEV_VMID:-}"

PROBE_URLS="${PROBE_URLS:-http://${PROD_HOST}:6090/ http://${PROD_HOST}:5238/healthzEndpoint http://${PROD_HOST}:5280/healthzEndpoint}"
PROBE_TIMEOUT="${PROBE_TIMEOUT:-5}"
SSH_USER="${SSH_USER:-eelkhair}"
SSH_KEY="${SSH_KEY:-/opt/watchdog/.ssh/id_ed25519}"
KUMA_PUSH_URL="${KUMA_PUSH_URL:-}"
COOLDOWN_SECS="${COOLDOWN_SECS:-300}"
COMPOSE_DIR="${COMPOSE_DIR:-/home/${SSH_USER}}"
COMPOSE_PROJECT="${COMPOSE_PROJECT:-job-board}"

mkdir -p "$STATE_DIR"

log() { printf '[watchdog] %s\n' "$*"; }

run() {
  if [[ "$DRY_RUN" == "1" ]]; then
    log "DRY-RUN: $*"
    return 0
  fi
  "$@"
}

# ── state I/O ─────────────────────────────────────────────────────────────
load_state() {
  fail_count=0
  last_action_ts=0
  last_action_level=0
  if [[ -f "$STATE_FILE" ]]; then
    # shellcheck disable=SC1090
    source "$STATE_FILE"
  fi
}

save_state() {
  cat > "$STATE_FILE" <<EOF
fail_count=${fail_count}
last_action_ts=${last_action_ts}
last_action_level=${last_action_level}
EOF
}

now() { date +%s; }

# ── notifications ─────────────────────────────────────────────────────────
kuma() {
  local status="$1" msg="$2"
  [[ -z "$KUMA_PUSH_URL" ]] && return 0
  # Kuma's pasted URL includes a default query string (?status=up&msg=OK&ping=).
  # Strip it before appending our own — leaving it in causes duplicate query
  # params, which Express parses as arrays; Kuma's status validator rejects the
  # array and records the heartbeat as DOWN with "[object Object]" as the msg.
  local base="${KUMA_PUSH_URL%%\?*}"
  local encoded_msg
  encoded_msg=$(printf '%s' "$msg" | jq -sRr @uri)
  local url="${base}?status=${status}&msg=${encoded_msg}&ping="
  curl -fsS --max-time 5 "$url" >/dev/null || log "kuma push failed (non-fatal)"
}

# ── probes ────────────────────────────────────────────────────────────────
probe_one() {
  local url="$1"
  curl -fsS --max-time "$PROBE_TIMEOUT" --retry 1 --retry-delay 1 -o /dev/null "$url"
}

probe_all() {
  local url ok=0 total=0
  for url in $PROBE_URLS; do
    total=$((total + 1))
    if probe_one "$url"; then
      ok=$((ok + 1))
    else
      log "probe FAIL: $url"
    fi
  done
  log "probes ok=${ok}/${total}"
  # Healthy = all probes pass. One flaky service counts as unhealthy; that's
  # intentional — the gateway being up but monolith down still means the user
  # gets a broken app.
  [[ "$ok" == "$total" ]]
}

# ── actions ───────────────────────────────────────────────────────────────
ssh_exec() {
  local host="$1"; shift
  run ssh -i "$SSH_KEY" \
    -o StrictHostKeyChecking=accept-new \
    -o ConnectTimeout=10 \
    -o BatchMode=yes \
    "${SSH_USER}@${host}" "$@"
}

# Poll SSH until it accepts a connection or the deadline passes.
wait_for_ssh() {
  local host="$1"
  local max_secs="${2:-180}"
  local start now_ts
  start=$(now)
  while :; do
    now_ts=$(now)
    if (( now_ts - start >= max_secs )); then
      return 1
    fi
    if ssh -i "$SSH_KEY" \
        -o StrictHostKeyChecking=accept-new \
        -o ConnectTimeout=5 \
        -o BatchMode=yes \
        "${SSH_USER}@${host}" 'true' 2>/dev/null; then
      return 0
    fi
    sleep 5
  done
}

# Enforce "dev stays off" policy — called after any prod recovery.
# If DEV_VMID is unset, no-op. If dev is already stopped, no-op. Otherwise
# hard-stops dev via PVE API so it can't compete for cluster resources.
ensure_dev_stopped() {
  [[ -z "$DEV_VMID" ]] && return 0
  local status
  status=$(get_vm_status "$DEV_VMID")
  if [[ "$status" == "stopped" ]]; then
    log "dev VM ${DEV_VMID} already stopped — policy OK"
    return 0
  fi
  log "policy: stopping dev VM ${DEV_VMID} (was ${status})"
  kuma down "policy: stop dev VMID=${DEV_VMID} (was ${status})"
  local code
  code=$(pve_post "/nodes/${PROD_NODE}/qemu/${DEV_VMID}/status/stop")
  if [[ "$code" =~ ^2 ]]; then
    log "dev stop: PVE returned HTTP ${code}"
  else
    log "dev stop: PVE returned HTTP ${code} — body: $(cat /tmp/pve.out 2>/dev/null | head -c 300)"
  fi
}

# Wait for SSH, then ensure the docker compose stack is up. Idempotent —
# `up -d` is a no-op when everything is already running with correct state.
post_vm_up() {
  log "waiting up to 180s for SSH on ${PROD_HOST}..."
  if [[ "$DRY_RUN" == "1" ]]; then
    log "DRY-RUN: would wait for SSH and run docker compose up -d"
    return 0
  fi
  if wait_for_ssh "$PROD_HOST" 180; then
    log "SSH up, running docker compose up -d"
    if ssh_exec "$PROD_HOST" "cd ${COMPOSE_DIR} && docker compose -p ${COMPOSE_PROJECT} up -d"; then
      log "compose up -d: OK"
    else
      log "compose up -d: FAILED (next tick will re-evaluate)"
    fi
  else
    log "SSH did not come up within 180s — next tick will reprobe"
  fi
}

# PVE API POST helper — returns HTTP code, body goes to /tmp/pve.out.
# --insecure because Proxmox ships with a self-signed cert by default.
pve_post() {
  local path="$1"
  local url="https://${PVE_API_HOST}/api2/json${path}"
  if [[ "$DRY_RUN" == "1" ]]; then
    log "DRY-RUN: POST $url"
    echo "200"
    return 0
  fi
  curl -sS --insecure -o /tmp/pve.out -w '%{http_code}' \
    -X POST "$url" \
    -H "Authorization: PVEAPIToken=${PVE_TOKEN_ID}=${PVE_TOKEN_SECRET}" \
    2>/dev/null || echo "000"
}

pve_get() {
  local path="$1"
  local url="https://${PVE_API_HOST}/api2/json${path}"
  curl -sS --insecure \
    -H "Authorization: PVEAPIToken=${PVE_TOKEN_ID}=${PVE_TOKEN_SECRET}" \
    "$url" 2>/dev/null
}

# Returns "running" | "stopped" | "paused" | "unknown".
get_vm_status() {
  local vmid="$1"
  local json
  json=$(pve_get "/nodes/${PROD_NODE}/qemu/${vmid}/status/current")
  printf '%s' "$json" | jq -r '.data.qmpstatus // .data.status // "unknown"' 2>/dev/null || echo "unknown"
}

# L1: first-try recovery — state-aware.
#   VM running & unhealthy  → docker compose up -d (containers bad, VM fine)
#   VM stopped/paused/etc.  → qm start + wait for SSH + docker compose up -d
action_l1_restart_or_start() {
  local status
  status=$(get_vm_status "$PROD_VMID")
  log "L1: prod VM ${PROD_VMID} status=${status}"

  if [[ "$status" == "running" ]]; then
    log "L1: docker compose up -d on ${PROD_HOST}"
    kuma down "L1 docker compose up -d on ${PROD_HOST}"
    if ssh_exec "$PROD_HOST" "cd ${COMPOSE_DIR} && docker compose -p ${COMPOSE_PROJECT} up -d"; then
      log "L1: compose up -d returned OK"
    else
      log "L1: compose up -d FAILED (SSH or compose error)"
    fi
  else
    log "L1: starting stopped VM via PVE API"
    kuma down "L1 qm start vmid=${PROD_VMID} (was ${status})"
    local code
    code=$(pve_post "/nodes/${PROD_NODE}/qemu/${PROD_VMID}/status/start")
    if [[ "$code" =~ ^2 ]]; then
      log "L1: PVE start returned HTTP ${code}"
      ensure_dev_stopped
      post_vm_up
    else
      log "L1: PVE start returned HTTP ${code} — body: $(cat /tmp/pve.out 2>/dev/null | head -c 300)"
    fi
  fi
}

# L2: harder recovery — force a VM reboot (or start if it's already stopped
# because L1 didn't stick). In both cases, follow up with compose up -d.
action_l2_vm_reboot() {
  local status
  status=$(get_vm_status "$PROD_VMID")
  log "L2: prod VM ${PROD_VMID} status=${status}"

  local endpoint="reboot"
  [[ "$status" != "running" ]] && endpoint="start"

  log "L2: qm ${endpoint} ${PROD_VMID} on node ${PROD_NODE}"
  kuma down "L2 qm ${endpoint} vmid=${PROD_VMID} node=${PROD_NODE}"
  local code
  code=$(pve_post "/nodes/${PROD_NODE}/qemu/${PROD_VMID}/status/${endpoint}")
  if [[ "$code" =~ ^2 ]]; then
    log "L2: PVE returned HTTP ${code} (task accepted)"
    ensure_dev_stopped
    post_vm_up
  else
    log "L2: PVE returned HTTP ${code} — body: $(cat /tmp/pve.out 2>/dev/null | head -c 300)"
  fi
}

action_l3_dev_poweroff_then_retry() {
  log "L3: poweroff ${DEV_HOST} (free cluster RAM), then retry L2"
  kuma down "L3 poweroff dev=${DEV_HOST}, retry L2"
  ssh_exec "$DEV_HOST" "sudo -n poweroff" || log "L3: dev poweroff SSH failed (continuing)"
  sleep 10
  action_l2_vm_reboot
}

# ── main ──────────────────────────────────────────────────────────────────
load_state

# FORCE_LEVEL override — verification only. Executes the level's action,
# updates state as if it just happened, and exits. Does NOT touch fail_count.
if [[ -n "${FORCE_LEVEL:-}" ]]; then
  log "FORCE_LEVEL=${FORCE_LEVEL} (verification run)"
  case "$FORCE_LEVEL" in
    1) action_l1_restart_or_start ;;
    2) action_l2_vm_reboot ;;
    3) action_l3_dev_poweroff_then_retry ;;
    *) echo "FORCE_LEVEL must be 1, 2, or 3" >&2; exit 2 ;;
  esac
  last_action_ts=$(now)
  last_action_level="$FORCE_LEVEL"
  save_state
  exit 0
fi

if probe_all; then
  if (( fail_count > 0 )); then
    log "recovered (fail_count was ${fail_count})"
    kuma up "recovered after ${fail_count} failed probes"
  else
    kuma up "healthy"
  fi
  fail_count=0
  save_state
  exit 0
fi

# Unhealthy path.
fail_count=$((fail_count + 1))
log "unhealthy, fail_count=${fail_count}"
save_state

# Respect cooldown — don't escalate again within COOLDOWN_SECS of the last
# action. Lets the previous action actually take effect before the next.
ts_now=$(now)
since_last_action=$((ts_now - last_action_ts))
if (( last_action_level > 0 && since_last_action < COOLDOWN_SECS )); then
  log "cooldown active (${since_last_action}s < ${COOLDOWN_SECS}s since L${last_action_level}), skipping escalation"
  exit 0
fi

# Escalation: each level fires once, then cooldown blocks the next until
# enough time has passed. Use >= with last_action_level tracking so a
# counter that climbed past a threshold during cooldown still escalates
# correctly (instead of getting skipped by an exact-match).
if (( fail_count >= 8 && last_action_level < 3 )); then
  action_l3_dev_poweroff_then_retry
  last_action_ts="$ts_now"
  last_action_level=3
  save_state
elif (( fail_count >= 5 && last_action_level < 2 )); then
  action_l2_vm_reboot
  last_action_ts="$ts_now"
  last_action_level=2
  save_state
elif (( fail_count >= 3 && last_action_level < 1 )); then
  action_l1_restart_or_start
  last_action_ts="$ts_now"
  last_action_level=1
  save_state
elif (( fail_count >= 10 )); then
  log "max escalation reached (fail_count=${fail_count}); logging only"
  kuma down "prod still down after all escalations; DNS failover should have triggered"
else
  log "debounce (fail_count=${fail_count}, thresholds at 3/5/8)"
fi
