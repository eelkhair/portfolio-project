#!/usr/bin/env bash
# watchdog-probe.sh — probe prod from outside the homelab and maintain
# .github/watchdog-state.json. Flips DNS via deploy/cloudflare/failover-landing.sh
# once sustained failure / sustained recovery is confirmed.
#
# State transitions (file: .github/watchdog-state.json):
#
#   mode=tunnel, probe fail      → consecutive_failures++
#   mode=tunnel, failures >= 2   → FLIP to cf, mode=pages, reset success counter
#   mode=pages,  probe success   → consecutive_successes++
#   mode=pages,  successes >= 6  → (also gate on tunnel-direct probe)
#                                  if tunnel reachable AND last_flip > 15 min ago
#                                  → FLIP to proxmox, mode=tunnel
#
# Why the tunnel-direct gate: the user-facing probe succeeds while we're
# already on Pages (Pages is serving the same site), so success alone
# doesn't prove the tunnel has actually recovered. We ALSO hit
# <tunnel-id>.cfargotunnel.com directly, which bypasses DNS and only
# responds if cloudflared on the prod host is connected.
#
# Required env (set by workflow):
#   CLOUDFLARE_API_TOKEN, CLOUDFLARE_ZONE_ID_TECH, CLOUDFLARE_TUNNEL_ID,
#   PAGES_PROJECT
set -euo pipefail

STATE_FILE="${STATE_FILE:-.github/watchdog-state.json}"
PROBE_URL="${PROBE_URL:-https://elkhair.tech/}"
FAILOVER_SCRIPT="${FAILOVER_SCRIPT:-deploy/cloudflare/failover-landing.sh}"

FAIL_THRESHOLD="${FAIL_THRESHOLD:-2}"          # 2 fails × 5 min ≈ 10 min
SUCCESS_THRESHOLD="${SUCCESS_THRESHOLD:-6}"     # 6 × 5 min = 30 min green
HYSTERESIS_SECS="${HYSTERESIS_SECS:-900}"       # 15 min between flips

: "${CLOUDFLARE_API_TOKEN:?}"
: "${CLOUDFLARE_TUNNEL_ID:?}"

if [[ ! -f "$STATE_FILE" ]]; then
  echo "::error::state file missing: $STATE_FILE"
  exit 1
fi

# ── Probes ────────────────────────────────────────────────────────────────
probe_public() {
  # Returns 0 if user-facing URL is 2xx.
  curl -fsS --max-time 10 --retry 2 --retry-delay 5 -o /dev/null "$PROBE_URL"
}

probe_tunnel_direct() {
  # Returns 0 if the Cloudflare tunnel origin is reachable (i.e. cloudflared
  # on the prod host is still connected). Hitting <uuid>.cfargotunnel.com
  # routes by Host header, so we provide the landing hostname. If the tunnel
  # is down this returns 530/502, which curl -f converts to non-zero exit.
  curl -fsS --max-time 10 -o /dev/null \
    -H "Host: elkhair.tech" \
    "https://${CLOUDFLARE_TUNNEL_ID}.cfargotunnel.com/"
}

# ── State helpers ─────────────────────────────────────────────────────────
jq_get()   { jq -r "$1" "$STATE_FILE"; }
jq_write() { jq "$1" "$STATE_FILE" > "$STATE_FILE.tmp" && mv "$STATE_FILE.tmp" "$STATE_FILE"; }

mode=$(jq_get '.mode')
fails=$(jq_get '.consecutive_failures')
succs=$(jq_get '.consecutive_successes')
last_flip=$(jq_get '.last_flip // empty')
now_iso=$(date -u +%Y-%m-%dT%H:%M:%SZ)
now_epoch=$(date -u +%s)

last_flip_epoch=0
if [[ -n "$last_flip" ]]; then
  last_flip_epoch=$(date -u -d "$last_flip" +%s 2>/dev/null || echo 0)
fi
since_flip=$(( now_epoch - last_flip_epoch ))

echo "mode=$mode failures=$fails successes=$succs since_last_flip=${since_flip}s"

# ── Probe + update counters ──────────────────────────────────────────────
if probe_public; then
  probe_ok=1
  echo "public probe: OK"
else
  probe_ok=0
  echo "public probe: FAIL"
fi

if [[ "$mode" == "tunnel" ]]; then
  if (( probe_ok )); then
    jq_write '.consecutive_failures = 0'
  else
    jq_write ".consecutive_failures = $((fails + 1)) | .consecutive_successes = 0"
    fails=$((fails + 1))
  fi
else
  # mode == pages
  if (( probe_ok )); then
    jq_write ".consecutive_successes = $((succs + 1))"
    succs=$((succs + 1))
  fi
  # A failed public probe while mode=pages means Pages itself is broken —
  # rare, nothing to auto-recover to. Reset success counter so we don't
  # flip back on stale greens.
  if ! (( probe_ok )); then
    jq_write '.consecutive_successes = 0'
    succs=0
  fi
fi

# ── Failover: tunnel → pages ─────────────────────────────────────────────
if [[ "$mode" == "tunnel" ]] && (( fails >= FAIL_THRESHOLD )); then
  echo "::warning::failover triggered — flipping to Cloudflare Pages"
  bash "$FAILOVER_SCRIPT" cf both
  jq_write ".mode = \"pages\" | .last_flip = \"$now_iso\" | .consecutive_failures = 0 | .consecutive_successes = 0"
  exit 0
fi

# ── Recovery: pages → tunnel ─────────────────────────────────────────────
if [[ "$mode" == "pages" ]] && (( succs >= SUCCESS_THRESHOLD )); then
  if (( since_flip < HYSTERESIS_SECS )); then
    echo "recovery pending: success threshold met but within ${HYSTERESIS_SECS}s hysteresis (since_flip=${since_flip}s)"
    exit 0
  fi
  if ! probe_tunnel_direct; then
    echo "recovery pending: public is green but tunnel-direct probe fails — cloudflared not confirmed healthy"
    exit 0
  fi
  echo "::notice::recovery — flipping back to Proxmox tunnel"
  bash "$FAILOVER_SCRIPT" proxmox both
  jq_write ".mode = \"tunnel\" | .last_flip = \"$now_iso\" | .consecutive_failures = 0 | .consecutive_successes = 0"
  exit 0
fi

exit 0
