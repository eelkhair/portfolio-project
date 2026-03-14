#!/bin/bash
set -euo pipefail

# ===== ENVIRONMENT SELECTION =====
echo "Select environment:"
echo "  1) dev"
echo "  2) prod"
echo "  3) both (dev + prod)"
read -p "Enter choice [1/2/3]: " ENV_CHOICE

DEPLOY_TARGETS=()

case "$ENV_CHOICE" in
  1|dev)
    DEPLOY_TARGETS+=("dev")
    ;;
  2|prod)
    DEPLOY_TARGETS+=("prod")
    echo "⚠️  You are about to deploy to PRODUCTION (192.168.1.112)."
    read -p "Are you sure? (y/N): " CONFIRM
    if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
      echo "Aborted."
      exit 0
    fi
    ;;
  3|both)
    DEPLOY_TARGETS+=("dev" "prod")
    echo "⚠️  You are about to deploy to BOTH dev AND prod."
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

echo "Will deploy to: ${DEPLOY_TARGETS[*]}"

# ===== ONE PASSWORD FOR EVERYTHING =====
read -s -p "🔐 Enter password (used for Docker registry): " PASSWORD
echo

echo "🔐 Logging into registry.eelkhair.net..."
printf '%s\n' "$PASSWORD" | docker login registry.eelkhair.net --username eelkhair --password-stdin

# ===== SERVICE DEFINITIONS =====
declare -A services=(
  ["job-admin"]="../apps/job-admin"
  ["job-public"]="../apps/job-public"
  ["ai-service"]="../services/ai-service"
  ["ai-service-v2"]="../services/ai-service.v2"
  ["ai-mcp-integration"]="../services/ai-service.v2"
  ["ai-mcp-micro"]="../services/ai-service.v2"
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

# ===== HELPER: resolve env-specific vars =====
env_vars() {
  local env="$1"
  case "$env" in
    dev)
      REMOTE_HOST="192.168.1.200"
      ADMIN_BUILD_CONFIG="development"
      PUBLIC_BUILD_CONFIG="development"
      ;;
    prod)
      REMOTE_HOST="192.168.1.112"
      ADMIN_BUILD_CONFIG="production"
      PUBLIC_BUILD_CONFIG="production"
      ;;
  esac
}

# ===== HELPER: build & push backend services (env-independent, run once) =====
build_and_push_backends() {
  for name in "${!services[@]}"; do
    # Skip Angular apps — they're built per-environment
    if [ "$name" = "job-admin" ] || [ "$name" = "job-public" ]; then
      continue
    fi

    path="${services[$name]}"
    image="registry.eelkhair.net/${name}:latest"

    if [ "$name" = "ai-service-v2" ]; then
      docker build \
        -f "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2/Src/Presentation/JobBoard.AI.API/Dockerfile" \
        -t "$image" \
        "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2"
    elif [ "$name" = "ai-mcp-integration" ]; then
      docker build \
        -f "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2/Src/Presentation/JobBoard.AI.MCP.Integration/Dockerfile" \
        -t "$image" \
        "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2"
    elif [ "$name" = "ai-mcp-micro" ]; then
      docker build \
        -f "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2/Src/Presentation/JobBoard.AI.MCP.Micro/Dockerfile" \
        -t "$image" \
        "/c/Users/elkha/RiderProjects/portfolio project/services/ai-service.v2"
    elif [ "$name" = "monolith-api" ]; then
      docker build \
        -f "/c/Users/elkha/RiderProjects/portfolio project/services/monolith/Src/Presentation/JobBoard.API/Dockerfile" \
        -t "$image" \
        "/c/Users/elkha/RiderProjects/portfolio project/services/monolith"
    else
      docker build -t "$image" "$path"
    fi

    echo "📤 Pushing $image to registry.eelkhair.net..."
    docker push "$image"
    echo "✅ $name done"
    echo "-----------------------------"
  done
}

# ===== HELPER: build & push Angular apps (env-specific) =====
build_and_push_frontends() {
  local admin_config="$1"
  local public_config="$2"

  for name in "job-admin" "job-public"; do
    path="${services[$name]}"
    image="registry.eelkhair.net/${name}:latest"

    if [ "$name" = "job-admin" ]; then
      docker build --build-arg BUILD_CONFIG="$admin_config" -t "$image" "$path"
    else
      docker build --build-arg BUILD_CONFIG="$public_config" -t "$image" "$path"
    fi

    echo "📤 Pushing $image to registry.eelkhair.net..."
    docker push "$image"
    echo "✅ $name ($admin_config) done"
    echo "-----------------------------"
  done
}

# ===== HELPER: deploy to remote host =====
deploy_remote() {
  local host="$1"
  local env="$2"

  echo "🚀 Deploying + cleaning up on $env ($host)..."

# NOTE: no quotes around EOF so variables expand and we pass $PASSWORD through.
ssh -tt eelkhair@${host}<<EOF
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
echo "✅ Remote cleanup complete for $env!"

exit 0
EOF
  # ssh -tt may return non-zero on clean exit; don't let set -e kill the loop
  local ssh_exit=$?
  if [ $ssh_exit -ne 0 ]; then
    echo "⚠️  SSH session for $env exited with code $ssh_exit (usually harmless with -tt)"
  fi
}

# ===== MAIN =====

# 1. Build backend services once (identical across environments)
echo ""
echo "=========================================="
echo "🔨 Building backend services (shared)..."
echo "=========================================="
build_and_push_backends

# 2. Per-environment: build frontends, push, deploy
for target in "${DEPLOY_TARGETS[@]}"; do
  env_vars "$target"

  echo ""
  echo "=========================================="
  echo "🔨 Building frontends for $target..."
  echo "=========================================="

  build_and_push_frontends "$ADMIN_BUILD_CONFIG" "$PUBLIC_BUILD_CONFIG"
  deploy_remote "$REMOTE_HOST" "$target" || true
done

# wipe locally
PASSWORD='' ; unset PASSWORD || true
echo "🎉 All done! Deployed to: ${DEPLOY_TARGETS[*]}"
