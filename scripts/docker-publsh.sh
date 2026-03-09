#!/bin/bash
set -euo pipefail

# ===== ENVIRONMENT SELECTION =====
echo "Select environment:"
echo "  1) dev"
echo "  2) prod"
read -p "Enter choice [1/2]: " ENV_CHOICE

case "$ENV_CHOICE" in
  1|dev)
    ENVIRONMENT="dev"
    REMOTE_HOST="192.168.1.200"
    ADMIN_BUILD_CONFIG="development"
    ;;
  2|prod)
    ENVIRONMENT="prod"
    REMOTE_HOST="192.168.1.112"
    ADMIN_BUILD_CONFIG="production"
    echo "⚠️  You are about to deploy to PRODUCTION ($REMOTE_HOST)."
    read -p "Are you sure? (y/N): " CONFIRM
    if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
      echo "Aborted."
      exit 0
    fi
    ;;
  *)
    echo "Invalid choice. Exiting."
    exit 1
    ;;
esac

echo "Deploying to $ENVIRONMENT ($REMOTE_HOST)..."

# ===== ONE PASSWORD FOR EVERYTHING =====
read -s -p "🔐 Enter password (used for Docker registry): " PASSWORD
echo

echo "🔐 Logging into registry.eelkhair.net..."
printf '%s\n' "$PASSWORD" | docker login registry.eelkhair.net --username eelkhair --password-stdin

# ===== BUILD & PUSH LOCALLY =====
declare -A services=(
  ["job-admin"]="../apps/job-admin"
  ["job-public"]="../apps/job-public"
  ["ai-service"]="../services/ai-service"
  ["ai-service-v2"]="../services/ai-service.v2"
  ["job-api"]="../services/micro-services/job-api"
  ["company-api"]="../services/micro-services/company-api"
  ["admin-api"]="../services/micro-services/admin-api"
  ["user-api"]="../services/micro-services/user-api"
  ["monolith-api"]="../services/monolith"
  ["connector-api"]="../services/connector-api"
  ["gateway"]="../services/gateway"
  ["health-check"]="../services/micro-services/HealthChecks"
  ["keycloak"]="../infrastructure/keycloak"
)

for name in "${!services[@]}"; do
  path="${services[$name]}"
  image="registry.eelkhair.net/${name}:latest"

if [ "$name" = "ai-service-v2" ]; then
  docker build \
    -f "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2/Src/Presentation/JobBoard.AI.API/Dockerfile" \
    -t "$image" \
    "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2"
elif [ "$name" = "monolith-api" ]; then
  docker build \
    -f "/c/Users/elkha/RiderProjects/portfolio project/services/monolith/Src/Presentation/JobBoard.API/Dockerfile" \
    -t "$image" \
    "/c/Users/elkha/RiderProjects/portfolio project/services/monolith"
elif [ "$name" = "job-admin" ]; then
  docker build \
    --build-arg BUILD_CONFIG="$ADMIN_BUILD_CONFIG" \
    -t "$image" \
    "$path"
else
  docker build -t "$image" "$path"
fi

  echo "📤 Pushing $image to registry.eelkhair.net..."
  docker push "$image"
  echo "✅ $name done"
  echo "-----------------------------"
done

echo "🚀 Deploying + cleaning up on remote host ($REMOTE_HOST)..."

# NOTE: no quotes around EOF so variables expand and we pass $PASSWORD through.
ssh -tt eelkhair@${REMOTE_HOST}<<EOF
set -euo pipefail

PASSWORD='${PASSWORD}'

# Helper to run commands with sudo using the same password (no prompt, no freeze)
SUDO() {
  if [ "\$(id -u)" -eq 0 ]; then
    "\$@"
  else
    # -S: read password from stdin, -p "": empty prompt to avoid clutter
    printf '%s\n' "\$PASSWORD" | sudo -S -p "" "\$@"
  fi
}

echo "🔐 Docker login on remote..."
printf '%s\n' "\$PASSWORD" | docker login registry.eelkhair.net --username eelkhair --password-stdin

echo "📦 Pulling + starting stack..."
docker compose -p job-board pull
docker compose -p job-board up -d --force-recreate --remove-orphans

echo "🧹 Post-deploy cleanup..."

echo "0️⃣ Disk usage (before):"
df -h || true
SUDO du -sh /var/snap/docker/common/var-lib-docker/* 2>/dev/null | sort -h || true

echo "1️⃣ Prune unused Docker artifacts..."
docker system prune -a --volumes -f || SUDO docker system prune -a --volumes -f

echo "2️⃣ Clean BuildKit caches..."
docker builder prune -a -f || SUDO docker builder prune -a -f
docker buildx prune -a -f --keep-storage 512MB || SUDO docker buildx prune -a -f --keep-storage 512MB

echo "3️⃣ Truncate oversized container logs..."
SUDO find /var/snap/docker/common/var-lib-docker/containers/ -name "*.log" -type f -size +50M -exec sh -c '> "{}"' \; 2>/dev/null || true

echo "4️⃣ Clean OS junk..."
SUDO journalctl --vacuum-time=3d || true
SUDO apt-get clean || true
SUDO rm -rf /var/cache/apt/archives/* || true

echo "🧾 Disk usage (after):"
df -h || true

# wipe secret in remote shell
PASSWORD='' ; unset PASSWORD || true
echo "✅ Remote cleanup complete!"

logout


# wipe locally too
PASSWORD='' ; unset PASSWORD || true
echo "🎉 All done! Deployed to ${ENVIRONMENT}."
EOF
