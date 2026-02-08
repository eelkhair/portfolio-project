#!/bin/bash
set -euo pipefail

# ===== ONE PASSWORD FOR EVERYTHING =====
read -s -p "🔐 Enter password (used for Docker registry and remote sudo): " PASSWORD
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
  ["health-check"]="../services/micro-services/HealthChecks"
)

for name in "${!services[@]}"; do
  path="${services[$name]}"
  image="registry.eelkhair.net/${name}:latest"

if [ "$name" = "ai-service-v2" ]; then
  docker build \
    -f "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2/Src/Presentation/JobBoard.AI.API/Dockerfile" \
    -t "$image" \
    "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2"
else
  if [ "$name" = "monolith-api" ]; then
    docker build \
      -f "/c/Users/elkha/RiderProjects/portfolio project/services/monolith/Src/Presentation/JobBoard.API/Dockerfile" \
      -t "$image" \
      "/c/Users/elkha/RiderProjects/portfolio project/services/monolith"
  else
    docker build -t "$image" "$path"
  fi
fi

  echo "📤 Pushing $image to registry.eelkhair.net..."
  docker push "$image"
  echo "✅ $name done"
  echo "-----------------------------"
done

echo "🚀 Deploying + cleaning up on remote host..."

# NOTE: no quotes around EOF so variables expand and we pass $PASSWORD through.
ssh -tt eelkhair@192.168.1.112 <<EOF
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
docker compose -p job-board up -d

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
echo "🎉 All done!"
EOF
