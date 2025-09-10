#!/bin/bash

set -e

echo "🔐 Enter Docker registry password for user 'eelkhair':"
read -s DOCKER_PASSWORD

echo "🔐 Logging into registry.eelkhair.net..."
echo "$DOCKER_PASSWORD" | docker login registry.eelkhair.net --username eelkhair --password-stdin

declare -A services=(
  ["job-admin"]="../apps/job-admin"
  ["job-public"]="../apps/job-public"
  ["ai-service"]="../services/ai-service"
  ["job-api"]="../services/job-api"
  ["company-api"]="../services/company-api"
  ["admin-api"]="../services/admin-api"
  ["user-api"]="../services/user-api"
)

for name in "${!services[@]}"; do
  path="${services[$name]}"
  image="registry.eelkhair.net/${name}:latest"

  echo "🔨 Building image for $name at $path..."
  docker build -t "$image" "$path"

  echo "📤 Pushing $image to registry.eelkhair.net..."
  docker push "$image"
  echo "✅ $name done"
  echo "-----------------------------"
done

echo "🚀 Starting containers using docker compose on remote host..."
ssh -tt eelkhair@192.168.1.112 <<EOF
set -euo pipefail

# login on the remote using the same password
echo "$DOCKER_PASSWORD" | docker login registry.eelkhair.net --username eelkhair --password-stdin

docker compose -p job-board pull
docker compose -p job-board up -d
exit
EOF

echo "✅ all done!"