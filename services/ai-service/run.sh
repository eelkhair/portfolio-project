#!/usr/bin/env bash
set -euo pipefail

# ========= Config =========
APP_ID="ai-service"
APP_PORT="${APP_PORT:-6082}"

# Liveness vs Readiness
LIVE_PATH="${LIVE_PATH:-/livez}"
READY_PATH="${READY_PATH:-/readyz}"
LIVE_URL="http://127.0.0.1:${APP_PORT}${LIVE_PATH}"
READY_URL="http://127.0.0.1:${APP_PORT}${READY_PATH}"

# Dapr ports
DAPR_HTTP_PORT="${DAPR_HTTP_PORT:-6083}"
DAPR_GRPC_PORT="${DAPR_GRPC_PORT:-46082}"
DAPR_PROFILE_PORT="${DAPR_PROFILE_PORT:-56082}"
LOG_LEVEL="${LOG_LEVEL:-debug}"

# Dapr resources/config
COMPONENTS_PATH="${COMPONENTS_PATH:-../../Components}"
# FIX: independent second folder (e.g., secrets)
COMPONENTS_PATH2="${COMPONENTS_PATH2:-Dapr/Components/Secrets}"
CONFIG_PATH="${CONFIG_PATH:-../../Config/config.yaml}"

# Optional: have daprd probe your app readiness directly
# set ENABLE_APP_HEALTH=true to enable these flags
APP_HEALTH_INTERVAL_MS="${APP_HEALTH_INTERVAL_MS:-2000}"
APP_HEALTH_TIMEOUT_MS="${APP_HEALTH_TIMEOUT_MS:-500}"
APP_HEALTH_THRESHOLD="${APP_HEALTH_THRESHOLD:-3}"

# ========= Helpers =========
die(){ echo "❌ $*" >&2; exit 1; }
have(){ command -v "$1" >/dev/null 2>&1; }

choose_start_cmd() {
  # Prefer a built artifact if present; else fall back to dev
  if [[ -f "dist/index.js" || -f "dist/main.js" ]]; then
    if jq -e '.scripts["start:prod"]' package.json >/dev/null 2>&1; then echo "npm run start:prod"; return; fi
    if jq -e '.scripts["start"]'     package.json >/dev/null 2>&1; then echo "npm start";         return; fi
    [[ -f "dist/index.js" ]] && { echo "node dist/index.js"; return; }
    [[ -f "dist/main.js"  ]] && { echo "node dist/main.js";  return; }
  fi
  if jq -e '.scripts["dev"]' package.json >/dev/null 2>&1; then echo "npm run dev"; return; fi
  die "No build and no dev script. Add a dev script or build to dist/index.js."
}

wait_for_200() {
  local url="$1" tries="${2:-60}" delay="${3:-0.5}"
  echo "⏳ Waiting for 200 from ${url} ..."
  for _ in $(seq 1 "$tries"); do
    local code
    code="$(curl -sS -o /dev/null -w '%{http_code}' "$url" || true)"
    if [[ "$code" == "200" ]]; then
      echo "✅ 200 from ${url}"
      return 0
    fi
    sleep "$delay"
  done
  echo "⚠️  No 200 from ${url} within timeout."
  return 1
}

cleanup(){
  echo -e "\n🧹 Stopping..."
  [[ -n "${DAPR_PID:-}" ]] && kill "$DAPR_PID" 2>/dev/null || true
  [[ -n "${APP_PID:-}"  ]] && kill "$APP_PID"  2>/dev/null || true
}
trap cleanup EXIT INT TERM

# ========= Pre-flight =========
have jq    || die "jq is required (reads package.json)."
have daprd || die "daprd not found. Install the Dapr CLI."

[[ -d "$COMPONENTS_PATH"  ]] || echo "⚠️  Missing ${COMPONENTS_PATH} (continuing)"
[[ -d "$COMPONENTS_PATH2" ]] || echo "⚠️  Missing ${COMPONENTS_PATH2} (continuing)"
[[ -f "$CONFIG_PATH"      ]] || echo "⚠️  Missing ${CONFIG_PATH} (continuing)"

# Auto-install deps for dev convenience
if [[ -f package.json && ! -d node_modules ]]; then
  echo "📦 Installing dependencies..."
  npm ci || npm install
fi

# ========= Start app =========
APP_CMD="$(choose_start_cmd)"
echo "▶️  Starting app: ${APP_CMD}"
bash -lc "${APP_CMD}" &
APP_PID=$!

# Wait for LIVENESS only (server listening)
wait_for_200 "$LIVE_URL" 120 0.5 || echo "ℹ️  Continuing even though liveness not OK yet."

# ========= Start Dapr =========
echo "🚀 Starting daprd (HTTP ${DAPR_HTTP_PORT}, gRPC ${DAPR_GRPC_PORT})..."

# Build optional app-health args
DAPR_APP_HEALTH_ARGS=()
if [[ "${ENABLE_APP_HEALTH:-false}" == "true" ]]; then
  DAPR_APP_HEALTH_ARGS+=(
    --enable-app-health-check
    --app-health-probe-interval "${APP_HEALTH_INTERVAL_MS}"
    --app-health-probe-timeout  "${APP_HEALTH_TIMEOUT_MS}"
    --app-health-threshold      "${APP_HEALTH_THRESHOLD}"
    --app-health-check-path     "${READY_PATH}"
  )
fi

daprd \
  --app-id         "${APP_ID}" \
  --app-port       "${APP_PORT}" \
  --dapr-http-port "${DAPR_HTTP_PORT}" \
  --dapr-grpc-port "${DAPR_GRPC_PORT}" \
  --profile-port   "${DAPR_PROFILE_PORT}" \
  --resources-path "${COMPONENTS_PATH}" \
  --resources-path "${COMPONENTS_PATH2}" \
  --config         "${CONFIG_PATH}" \
  --log-level      "${LOG_LEVEL}" \
  "${DAPR_APP_HEALTH_ARGS[@]}" &
DAPR_PID=$!

# (Optional) Wait for READINESS after daprd is up
wait_for_200 "$READY_URL" 240 1 || echo "ℹ️  App not ready yet; Dapr started anyway."

cat <<EOF

✅ ${APP_ID} running
   App:   http://localhost:${APP_PORT}
          Live:  ${LIVE_URL}
          Ready: ${READY_URL}
   Dapr:  http://localhost:${DAPR_HTTP_PORT}  (gRPC ${DAPR_GRPC_PORT})

Press Ctrl+C to stop.
EOF

# Block on children
wait
