#!/usr/bin/env bash
set -e

APP_PORT=6082
DAPR_HTTP_PORT=6083
DAPR_GRPC_PORT=46082
APP_ID="ai-service"

# --------------------------------------------------------
# Kill any already-running Dapr or Node instances
# --------------------------------------------------------
echo "🧹 Cleaning old processes..."
pkill -f "daprd --app-id ${APP_ID}" 2>/dev/null || true
pkill -f "node" 2>/dev/null || true
sleep 1

# --------------------------------------------------------
# Function: wait for Dapr HTTP endpoint
# --------------------------------------------------------
wait_for_dapr() {
  echo "⏳ Waiting for Dapr HTTP endpoint on http://127.0.0.1:${DAPR_HTTP_PORT}/v1.0/metadata ..."

  for i in {1..40}; do
    if curl -sf "http://127.0.0.1:${DAPR_HTTP_PORT}/v1.0/metadata" >/dev/null 2>&1; then
      echo "✅ Dapr sidecar is responding."
      return 0
    fi
    sleep 1
  done

  echo "❌ Dapr did not start in time."
  exit 1
}

# --------------------------------------------------------
# Start Dapr sidecar
# --------------------------------------------------------
echo "🚀 Starting Dapr sidecar..."

daprd \
  --app-id ${APP_ID} \
  --app-port ${APP_PORT} \
  --dapr-http-port ${DAPR_HTTP_PORT} \
  --dapr-grpc-port ${DAPR_GRPC_PORT} \
  --resources-path ../../Components \
  --resources-path ./Dapr/Components/Secrets \
  --resources-path ./Dapr/Components/Events \
  --config ../../Config/config.yaml \
  --log-level debug &

DAPR_PID=$!
sleep 1

# --------------------------------------------------------
# Wait for Dapr to become ready
# --------------------------------------------------------
wait_for_dapr
echo "🔥 Dapr is ready."

# --------------------------------------------------------
# Start the actual app
# --------------------------------------------------------
echo "▶️ Starting AI Service (npm run dev) ..."
npm run dev &

APP_PID=$!

echo ""
echo "🎉 AI Service + Dapr running."
echo "   App URL:  http://localhost:${APP_PORT}"
echo "   Dapr URL: http://localhost:${DAPR_HTTP_PORT}/v1.0"
echo ""
echo "Press Ctrl+C to stop both services."
echo ""

# --------------------------------------------------------
# Shutdown trap
# --------------------------------------------------------
cleanup() {
  echo ""
  echo "🛑 Shutting down AI Service + Dapr..."
  kill ${APP_PID} 2>/dev/null || true
  kill ${DAPR_PID} 2>/dev/null || true
  exit 0
}

trap cleanup INT

# --------------------------------------------------------
# Keep script alive
# --------------------------------------------------------
wait
