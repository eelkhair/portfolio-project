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

PROBE_URLS="${PROBE_URLS:-http://${PROD_HOST}:6090/ http://${PROD_HOST}:5238/healthzEndpoint http://${PROD_HOST}:5280/healthzEndpoint}"
PROBE_TIMEOUT="${PROBE_TIMEOUT:-5}"
SSH_USER="${SSH_USER:-eelkhair}"
SSH_KEY="${SSH_KEY:-/opt/watchdog/.ssh/id_ed25519}"
KUMA_PUSH_URL="${KUMA_PUSH_URL:-}"
COOLDOWN_SECS="${COOLDOWN_SECS:-300}"
COMPOSE_DIR="${COMPOSE_DIR:-/home/${SSH_USER}}"

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

action_l1_docker_restart() {
  log "L1: docker compose restart on ${PROD_HOST}"
  kuma down "L1 docker compose restart on ${PROD_HOST}"
  if ssh_exec "$PROD_HOST" "cd ${COMPOSE_DIR} && docker compose restart"; then
    log "L1: restart command returned OK"
  else
    log "L1: restart command FAILED (SSH or compose error)"
  fi
}

action_l2_vm_reboot() {
  log "L2: qm reboot ${PROD_VMID} on node ${PROD_NODE}"
  kuma down "L2 qm reboot vmid=${PROD_VMID} node=${PROD_NODE}"
  local url="https://${PVE_API_HOST}/api2/json/nodes/${PROD_NODE}/qemu/${PROD_VMID}/status/reboot"
  if [[ "$DRY_RUN" == "1" ]]; then
    log "DRY-RUN: POST $url"
    return 0
  fi
  # --insecure — Proxmox installs ship with a self-signed cert by default.
  # Operator can install a CA cert and drop --insecure later.
  local code
  code=$(curl -sS --insecure -o /tmp/pve-reboot.out -w '%{http_code}' \
    -X POST "$url" \
    -H "Authorization: PVEAPIToken=${PVE_TOKEN_ID}=${PVE_TOKEN_SECRET}" || echo 000)
  if [[ "$code" =~ ^2 ]]; then
    log "L2: PVE returned HTTP ${code} (task accepted)"
  else
    log "L2: PVE returned HTTP ${code} — body: $(cat /tmp/pve-reboot.out 2>/dev/null | head -c 300)"
  fi
}

action_l3_dev_poweroff_then_retry() {
  log "L3: poweroff ${DEV_HOST} (free cluster RAM), then retry L2"
  kuma down "L3 poweroff dev=${DEV_HOST}, retry qm reboot prod"
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
    1) action_l1_docker_restart ;;
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

case "$fail_count" in
  3)
    action_l1_docker_restart
    last_action_ts="$ts_now"
    last_action_level=1
    save_state
    ;;
  5)
    action_l2_vm_reboot
    last_action_ts="$ts_now"
    last_action_level=2
    save_state
    ;;
  8)
    action_l3_dev_poweroff_then_retry
    last_action_ts="$ts_now"
    last_action_level=3
    save_state
    ;;
  *)
    if (( fail_count >= 10 )); then
      log "max escalation reached (fail_count=${fail_count}); logging only"
      kuma down "prod still down after all escalations; DNS failover should have triggered"
    else
      log "debounce (fail_count=${fail_count}, next threshold at 3/5/8)"
    fi
    ;;
esac
