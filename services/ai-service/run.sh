#!/usr/bin/env bash
set -euo pipefail

# ==== Fixed config to match docker-compose =====
APP_ID="ai-service"
APP_PORT="6082"          # service port
DAPR_HTTP_PORT="6083"    # Dapr HTTP port
DAPR_GRPC_PORT="46082"   # Dapr gRPC (free port for local)
DAPR_PROFILE_PORT="56082"
LOG_LEVEL="debug"
COMPONENTS_PATH="../Components"
CONFIG_PATH="../Config/config.yaml"

# ---- helpers ----
die(){ echo "❌ $*" >&2; exit 1; }
have(){ command -v "$1" >/dev/null 2>&1; }

choose_start_cmd() {
  # prefer start:prod -> start -> dev
  if [[ -f package.json ]]; then
    if jq -e '.scripts["start:prod"]' package.json >/dev/null 2>&1; then
      echo "npm run start:prod"; return
    elif jq -e '.scripts["start"]' package.json >/dev/null 2>&1; then
      echo "npm start"; return
    elif jq -e '.scripts["dev"]' package.json >/dev/null 2>&1; then
      echo "npm run dev"; return
    fi
  fi
  # fallback
  if [[ -f "dist/main.js" ]]; then
    echo "node dist/main.js"; return
  fi
  die "Could not infer start command. Add a start script in package.json or build to dist/main.js."
}

wait_for_port() {
  local port="$1" tries=60
  echo "⏳ Waiting for port ${port}..."
  for _ in $(seq 1 $tries); do
    if curl -fsS "http://127.0.0.1:${port}" >/dev/null 2>&1; then return 0; fi
    sleep 0.5
  done
  return 1
}

# ---- checks ----
have jq    || die "jq is required (reads package.json)."
have daprd || die "daprd not found. Install the Dapr CLI."
[[ -d "$COMPONENTS_PATH" ]] || echo "⚠️  Missing ${COMPONENTS_PATH} (continuing)"
[[ -f "$CONFIG_PATH"     ]] || echo "⚠️  Missing ${CONFIG_PATH} (continuing)"

# ---- deps (only if node project and node_modules missing) ----
if [[ -f package.json && ! -d node_modules ]]; then
  echo "📦 Installing dependencies..."
  npm ci || npm install
fi

# ---- start app ----
APP_CMD="$(choose_start_cmd)"
echo "▶️  Starting app: ${APP_CMD}"
bash -lc "${APP_CMD}" &
APP_PID=$!

cleanup(){
  echo -e "\n🧹 Stopping..."
  [[ -n "${DAPR_PID:-}" && -e /proc/$DAPR_PID ]] && kill "$DAPR_PID" || true
  [[ -e /proc/$APP_PID ]] && kill "$APP_PID" || true
}
trap cleanup EXIT INT TERM

if ! wait_for_port "$APP_PORT"; then
  echo "⚠️  App hasn't opened ${APP_PORT} yet; starting daprd anyway."
fi

# ---- start dapr sidecar ----
echo "🚀 Starting daprd (HTTP ${DAPR_HTTP_PORT}, gRPC ${DAPR_GRPC_PORT})..."
daprd \
  --app-id         "${APP_ID}" \
  --app-port       "${APP_PORT}" \
  --dapr-http-port "${DAPR_HTTP_PORT}" \
  --dapr-grpc-port "${DAPR_GRPC_PORT}" \
  --profile-port   "${DAPR_PROFILE_PORT}" \
  --resources-path "${COMPONENTS_PATH}" \
  --config         "${CONFIG_PATH}" \
  --log-level      "${LOG_LEVEL}" &

DAPR_PID=$!

echo "✅ ai-service running
   App:  http://localhost:${APP_PORT}
   Dapr: http://localhost:${DAPR_HTTP_PORT}  (gRPC ${DAPR_GRPC_PORT})
Press Ctrl+C to stop."
wait
