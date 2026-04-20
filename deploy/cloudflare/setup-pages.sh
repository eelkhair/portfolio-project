#!/usr/bin/env bash
# setup-pages.sh — idempotent Cloudflare Pages bootstrap for the landing page.
#
# One-time prerequisite (the only thing you can't automate): create a CF API
# token with `Account:Cloudflare Pages:Edit` + `Zone:DNS:Edit` scopes in the
# dashboard, save it as CLOUDFLARE_PAGES_API_TOKEN.
#
# Everything else below is API-driven and re-runnable. Use it to initially
# provision the project, rotate secrets later, or re-attach the custom domain
# if it drops off.
#
# Usage:
#   CLOUDFLARE_PAGES_API_TOKEN=...  \
#   CLOUDFLARE_ACCOUNT_ID=...       \
#   CLOUDFLARE_ZONE_ID_TECH=...     \
#   RESEND_API_KEY=...              \
#   EMAIL_FROM='Portfolio Contact <contact@elkhair.tech>' \
#   CONTACT_EMAIL=you@example.com   \
#   TURNSTILE_SECRET_KEY=...        \
#   NEXT_PUBLIC_TURNSTILE_SITE_KEY=... \
#   NEXT_PUBLIC_FARO_URL=https://faro.elkhair.tech/collect \
#   FEATURE_FLAGS_URL=https://job-monolith.elkhair.tech/api/public/feature-flags \
#   ./setup-pages.sh [project_name] [backup_hostname]
#
# Defaults: project_name=landing, backup_hostname=landing-backup.elkhair.tech

set -euo pipefail

PROJECT_NAME="${1:-landing}"
BACKUP_HOSTNAME="${2:-landing-backup.elkhair.tech}"
PRODUCTION_BRANCH="master"

: "${CLOUDFLARE_PAGES_API_TOKEN:?CLOUDFLARE_PAGES_API_TOKEN must be set (Pages:Edit + DNS:Edit)}"
: "${CLOUDFLARE_ACCOUNT_ID:?CLOUDFLARE_ACCOUNT_ID must be set}"
: "${CLOUDFLARE_ZONE_ID_TECH:?CLOUDFLARE_ZONE_ID_TECH must be set}"

# Runtime env vars for the Pages project. NEXT_PUBLIC_* are ALSO built into
# the static bundle by GitHub Actions, but mirror them here so local
# `wrangler pages deploy` works without extra flags.
: "${RESEND_API_KEY:?RESEND_API_KEY must be set}"
: "${EMAIL_FROM:?EMAIL_FROM must be set}"
: "${CONTACT_EMAIL:?CONTACT_EMAIL must be set}"
: "${TURNSTILE_SECRET_KEY:?TURNSTILE_SECRET_KEY must be set}"
: "${NEXT_PUBLIC_TURNSTILE_SITE_KEY:?NEXT_PUBLIC_TURNSTILE_SITE_KEY must be set}"
: "${NEXT_PUBLIC_FARO_URL:?NEXT_PUBLIC_FARO_URL must be set}"
: "${FEATURE_FLAGS_URL:?FEATURE_FLAGS_URL must be set}"
NEXT_PUBLIC_FARO_APP_NAME="${NEXT_PUBLIC_FARO_APP_NAME:-landing}"

CF_API="https://api.cloudflare.com/client/v4"
AUTH=(-H "Authorization: Bearer ${CLOUDFLARE_PAGES_API_TOKEN}" -H "Content-Type: application/json")

log() { printf '\n▸ %s\n' "$*"; }

# ─────────────────────────────────────────────────────────────────────────────
# 1. Create Pages project if it doesn't exist
# ─────────────────────────────────────────────────────────────────────────────
log "Checking Pages project '${PROJECT_NAME}'"
PROJECT_RESP=$(curl -sS "${CF_API}/accounts/${CLOUDFLARE_ACCOUNT_ID}/pages/projects/${PROJECT_NAME}" "${AUTH[@]}")
PROJECT_EXISTS=$(echo "$PROJECT_RESP" | jq -r '.success')

if [[ "$PROJECT_EXISTS" != "true" ]]; then
  log "Creating Pages project '${PROJECT_NAME}'"
  CREATE_BODY=$(jq -n --arg name "$PROJECT_NAME" --arg branch "$PRODUCTION_BRANCH" '{
    name: $name,
    production_branch: $branch
  }')
  CREATE_RESP=$(curl -sS -X POST "${CF_API}/accounts/${CLOUDFLARE_ACCOUNT_ID}/pages/projects" \
    "${AUTH[@]}" -d "$CREATE_BODY")
  if [[ "$(echo "$CREATE_RESP" | jq -r '.success')" != "true" ]]; then
    echo "Failed to create project:"
    echo "$CREATE_RESP" | jq .
    exit 1
  fi
  echo "  ✓ Created"
else
  echo "  ✓ Already exists"
fi

# ─────────────────────────────────────────────────────────────────────────────
# 2. Upsert env vars (production deployment config)
#    Secrets go in as secret_text (encrypted at rest, not returned on GET);
#    plain values go in as plain_text.
# ─────────────────────────────────────────────────────────────────────────────
log "Upserting env vars on '${PROJECT_NAME}' production"

ENV_VARS=$(jq -n \
  --arg resend "$RESEND_API_KEY" \
  --arg emailFrom "$EMAIL_FROM" \
  --arg contactEmail "$CONTACT_EMAIL" \
  --arg turnstileSecret "$TURNSTILE_SECRET_KEY" \
  --arg turnstileSite "$NEXT_PUBLIC_TURNSTILE_SITE_KEY" \
  --arg faroUrl "$NEXT_PUBLIC_FARO_URL" \
  --arg faroApp "$NEXT_PUBLIC_FARO_APP_NAME" \
  --arg flagsUrl "$FEATURE_FLAGS_URL" \
  '{
    RESEND_API_KEY:                 { type: "secret_text", value: $resend },
    EMAIL_FROM:                     { type: "plain_text",  value: $emailFrom },
    CONTACT_EMAIL:                  { type: "plain_text",  value: $contactEmail },
    TURNSTILE_SECRET_KEY:           { type: "secret_text", value: $turnstileSecret },
    NEXT_PUBLIC_TURNSTILE_SITE_KEY: { type: "plain_text",  value: $turnstileSite },
    NEXT_PUBLIC_FARO_URL:           { type: "plain_text",  value: $faroUrl },
    NEXT_PUBLIC_FARO_APP_NAME:      { type: "plain_text",  value: $faroApp },
    FEATURE_FLAGS_URL:              { type: "plain_text",  value: $flagsUrl }
  }')

PATCH_BODY=$(jq -n --argjson env "$ENV_VARS" '{
  deployment_configs: {
    production: {
      compatibility_flags: ["nodejs_compat"],
      compatibility_date: "2026-04-01",
      env_vars: $env
    }
  }
}')

PATCH_RESP=$(curl -sS -X PATCH "${CF_API}/accounts/${CLOUDFLARE_ACCOUNT_ID}/pages/projects/${PROJECT_NAME}" \
  "${AUTH[@]}" -d "$PATCH_BODY")

if [[ "$(echo "$PATCH_RESP" | jq -r '.success')" != "true" ]]; then
  echo "Failed to update env vars:"
  echo "$PATCH_RESP" | jq .
  exit 1
fi
echo "  ✓ 8 env vars applied (4 plain, 2 build-public, 2 secret)"

# ─────────────────────────────────────────────────────────────────────────────
# 3. Attach custom backup hostname
#    Pages API validates ownership via the zone that already contains the
#    apex. `landing-backup.elkhair.tech` lives under the elkhair.tech zone.
# ─────────────────────────────────────────────────────────────────────────────
log "Ensuring custom domain '${BACKUP_HOSTNAME}' is attached"

DOMAIN_CHECK=$(curl -sS "${CF_API}/accounts/${CLOUDFLARE_ACCOUNT_ID}/pages/projects/${PROJECT_NAME}/domains/${BACKUP_HOSTNAME}" "${AUTH[@]}")
DOMAIN_EXISTS=$(echo "$DOMAIN_CHECK" | jq -r '.success')

if [[ "$DOMAIN_EXISTS" != "true" ]]; then
  ADD_RESP=$(curl -sS -X POST "${CF_API}/accounts/${CLOUDFLARE_ACCOUNT_ID}/pages/projects/${PROJECT_NAME}/domains" \
    "${AUTH[@]}" -d "$(jq -n --arg name "$BACKUP_HOSTNAME" '{name: $name}')")
  if [[ "$(echo "$ADD_RESP" | jq -r '.success')" != "true" ]]; then
    echo "Failed to attach domain:"
    echo "$ADD_RESP" | jq .
    exit 1
  fi
  echo "  ✓ Attached. CF will provision the TLS cert (~30-60s)."
else
  STATUS=$(echo "$DOMAIN_CHECK" | jq -r '.result.status // "unknown"')
  echo "  ✓ Already attached (status: ${STATUS})"
fi

# ─────────────────────────────────────────────────────────────────────────────
# 4. Ensure a CNAME exists for the backup hostname → <project>.pages.dev
#    Without this, the custom domain attach succeeds but DNS doesn't route.
# ─────────────────────────────────────────────────────────────────────────────
log "Ensuring DNS CNAME for '${BACKUP_HOSTNAME}'"

CNAME_TARGET="${PROJECT_NAME}.pages.dev"
EXISTING=$(curl -sS "${CF_API}/zones/${CLOUDFLARE_ZONE_ID_TECH}/dns_records?type=CNAME&name=${BACKUP_HOSTNAME}" "${AUTH[@]}")
RECORD_ID=$(echo "$EXISTING" | jq -r '.result[0].id // empty')

DNS_BODY=$(jq -n --arg name "$BACKUP_HOSTNAME" --arg target "$CNAME_TARGET" \
  '{type:"CNAME", name:$name, content:$target, ttl:300, proxied:true}')

if [[ -n "$RECORD_ID" ]]; then
  curl -sS -X PATCH "${CF_API}/zones/${CLOUDFLARE_ZONE_ID_TECH}/dns_records/${RECORD_ID}" \
    "${AUTH[@]}" -d "$DNS_BODY" >/dev/null
  echo "  ✓ Updated ${BACKUP_HOSTNAME} → ${CNAME_TARGET}"
else
  curl -sS -X POST "${CF_API}/zones/${CLOUDFLARE_ZONE_ID_TECH}/dns_records" \
    "${AUTH[@]}" -d "$DNS_BODY" >/dev/null
  echo "  ✓ Created ${BACKUP_HOSTNAME} → ${CNAME_TARGET}"
fi

# ─────────────────────────────────────────────────────────────────────────────
# Done
# ─────────────────────────────────────────────────────────────────────────────
cat <<EOF

Cloudflare Pages is configured. Next:
  1. Push to master (or trigger deploy-landing-cf.yml manually). First deploy
     will populate ${PROJECT_NAME}.pages.dev and ${BACKUP_HOSTNAME}.
  2. Verify:   curl -sSI https://${BACKUP_HOSTNAME}/ | head -5
               curl -sS  https://${BACKUP_HOSTNAME}/api/status | jq .
  3. During outage:  ./failover-landing.sh cf dev
     (Flips dev.elkhair.tech CNAME to ${CNAME_TARGET}.)
EOF
