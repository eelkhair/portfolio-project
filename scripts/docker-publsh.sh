#!/bin/bash
set -uo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

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

# ===== SERVICE DEFINITIONS (ordered array for deterministic builds) =====
# Each entry: "name|context|dockerfile_flag"
#   dockerfile_flag: "default" = Dockerfile in context root
#                    path     = custom Dockerfile relative to context
BACKEND_SERVICES=(
  "gateway|../services/gateway|default"
  "monolith-api|../services/monolith|Src/Presentation/JobBoard.API/Dockerfile"
  "monolith-mcp|../services/monolith|Src/Presentation/JobBoard.API.Mcp/Dockerfile"
  "ai-service-v2|../services/ai-service.v2|Src/Presentation/JobBoard.AI.API/Dockerfile"
  "admin-api|../services/micro-services/admin-api|default"
  "admin-api-mcp|../services/micro-services/admin-api|AdminApi.Mcp/Dockerfile"
  "company-api|../services/micro-services/company-api|default"
  "job-api|../services/micro-services/job-api|default"
  "user-api|../services/micro-services/user-api|default"
  "connector-api|../services/connector-api|default"
  "reverse-connector-api|../services/reverse-connector-api|default"
  "health-check|../services/micro-services/HealthChecks|default"
  "keycloak|../infrastructure/keycloak|default"
  "landing|../apps/landing-next|default"
)

FAILED_BUILDS=()
SUCCESSFUL_BUILDS=()

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

# ===== HELPER: build & push one service =====
build_one() {
  local name="$1"
  local context="$2"
  local dockerfile_flag="$3"
  local image="registry.eelkhair.net/${name}:latest"

  echo ""
  echo "🔨 Building $name..."

  if [ "$dockerfile_flag" = "default" ]; then
    docker build -t "$image" "$context"
  else
    docker build -f "$context/$dockerfile_flag" -t "$image" "$context"
  fi

  if [ $? -ne 0 ]; then
    echo "❌ BUILD FAILED: $name"
    FAILED_BUILDS+=("$name")
    return 1
  fi

  echo "📤 Pushing $image..."
  docker push "$image"

  if [ $? -ne 0 ]; then
    echo "❌ PUSH FAILED: $name"
    FAILED_BUILDS+=("$name")
    return 1
  fi

  SUCCESSFUL_BUILDS+=("$name")
  echo "✅ $name done"
  echo "-----------------------------"
}

# ===== HELPER: build & push backend services =====
build_and_push_backends() {
  for entry in "${BACKEND_SERVICES[@]}"; do
    IFS='|' read -r name context dockerfile_flag <<< "$entry"
    build_one "$name" "$context" "$dockerfile_flag" || true
  done
}

# ===== HELPER: build & push Angular apps (env-specific tags) =====
build_and_push_frontends() {
  local admin_config="$1"
  local public_config="$2"
  local env_tag="$3"  # "dev" or "prod"

  for name in "job-admin" "job-public"; do
    local image="registry.eelkhair.net/${name}:${env_tag}"
    local config

    if [ "$name" = "job-admin" ]; then
      config="$admin_config"
      docker build --build-arg BUILD_CONFIG="$config" -t "$image" "../apps/job-admin"
    else
      config="$public_config"
      docker build --build-arg BUILD_CONFIG="$config" -t "$image" "../apps/job-public"
    fi

    if [ $? -ne 0 ]; then
      echo "❌ BUILD FAILED: $name ($config)"
      FAILED_BUILDS+=("$name")
      continue
    fi

    echo "📤 Pushing $image..."
    docker push "$image"

    if [ $? -ne 0 ]; then
      echo "❌ PUSH FAILED: $name"
      FAILED_BUILDS+=("$name")
      continue
    fi

    SUCCESSFUL_BUILDS+=("$name")
    echo "✅ $name ($config → :${env_tag}) done"
    echo "-----------------------------"
  done
}

# ===== HELPER: deploy to remote host =====
deploy_remote() {
  local host="$1"
  local env="$2"

  echo "🚀 Deploying + cleaning up on $env ($host)..."

  local compose_file="${SCRIPT_DIR}/docker-compose.${env}.yml"
  echo "📄 Uploading compose file to ${host}..."
  scp "${compose_file}" "eelkhair@${host}:/home/eelkhair/docker-compose.yml"

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
docker compose -p job-board up -d --remove-orphans

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

  build_and_push_frontends "$ADMIN_BUILD_CONFIG" "$PUBLIC_BUILD_CONFIG" "$target"
  deploy_remote "$REMOTE_HOST" "$target" || true
done

# ===== SUMMARY =====
echo ""
echo "=========================================="
echo "📊 Build Summary"
echo "=========================================="
echo "✅ Succeeded (${#SUCCESSFUL_BUILDS[@]}):"
for s in "${SUCCESSFUL_BUILDS[@]}"; do
  echo "   - $s"
done

if [ ${#FAILED_BUILDS[@]} -gt 0 ]; then
  echo ""
  echo "❌ Failed (${#FAILED_BUILDS[@]}):"
  for f in "${FAILED_BUILDS[@]}"; do
    echo "   - $f"
  done
  echo ""
  echo "⚠️  Some services failed to build. Deploy proceeded with available images."
fi

# wipe locally
PASSWORD='' ; unset PASSWORD || true
echo ""
echo "🎉 All done! Deployed to: ${DEPLOY_TARGETS[*]}"
