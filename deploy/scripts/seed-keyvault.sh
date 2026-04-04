#!/bin/bash
# ══════════════════════════════════════════════════════════════════════════════
# Seed Azure Key Vault with application secrets
# Run this after infrastructure deployment, before deploying apps.
#
# Usage: ./seed-keyvault.sh <vault-name>
# ══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

VAULT_NAME="${1:?Usage: $0 <vault-name>}"

echo "Seeding Key Vault: $VAULT_NAME"

# Helper function
set_secret() {
  local name="$1"
  local value="$2"
  az keyvault secret set --vault-name "$VAULT_NAME" --name "$name" --value "$value" --output none
  echo "  Set: $name"
}

# ── Keycloak Service Client (for Dapr service-to-service auth) ──
set_secret "Keycloak--ServiceClientId" "dapr-service-client"
set_secret "Keycloak--ServiceClientSecret" "${KEYCLOAK_SERVICE_CLIENT_SECRET:?Set KEYCLOAK_SERVICE_CLIENT_SECRET}"

# ── AI API Keys ──
set_secret "AI--CLAUDE-API-KEY" "${CLAUDE_API_KEY:-}"
set_secret "OpenAI--ApiKey" "${OPENAI_API_KEY:-}"

# ── RabbitMQ (used by monolith's direct CloudEventsPublisher) ──
# Not needed — monolith connects to RabbitMQ Container App via env var in Bicep

echo ""
echo "Key Vault seeded successfully."
echo "Connection strings for SQL, PostgreSQL, Redis, Storage are injected via Bicep env vars."
echo "Only application-level secrets (API keys, client credentials) are stored in Key Vault."
