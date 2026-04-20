#!/usr/bin/env bash
# failover-landing.sh — flip landing DNS between Proxmox tunnel and Cloudflare
# Pages during an outage.
#
# Usage:
#   CLOUDFLARE_API_TOKEN=... \
#   CLOUDFLARE_ZONE_ID_TECH=... \
#   CLOUDFLARE_TUNNEL_ID=... \
#   PAGES_PROJECT=landing \
#   ./failover-landing.sh <cf|proxmox> [dev|prod|both]
#
# cf       — point landing at Cloudflare Pages (use during outage)
# proxmox  — point landing back at the tunnel (normal state)
#
# Hostnames affected:
#   dev   → dev.elkhair.tech
#   prod  → elkhair.tech (apex), www.elkhair.tech
#   both  → all of the above (default)
#
# The script is idempotent — it upserts CNAMEs, so re-runs are safe.

set -euo pipefail

MODE="${1:-}"
SCOPE="${2:-both}"

if [[ "$MODE" != "cf" && "$MODE" != "proxmox" ]]; then
  echo "Usage: $0 <cf|proxmox> [dev|prod|both]" >&2
  exit 2
fi

: "${CLOUDFLARE_API_TOKEN:?CLOUDFLARE_API_TOKEN must be set}"
: "${CLOUDFLARE_ZONE_ID_TECH:?CLOUDFLARE_ZONE_ID_TECH must be set}"
: "${CLOUDFLARE_TUNNEL_ID:?CLOUDFLARE_TUNNEL_ID must be set}"
PAGES_PROJECT="${PAGES_PROJECT:-landing}"

TUNNEL_TARGET="${CLOUDFLARE_TUNNEL_ID}.cfargotunnel.com"
PAGES_TARGET="${PAGES_PROJECT}.pages.dev"

case "$MODE" in
  cf)      TARGET="$PAGES_TARGET" ;;
  proxmox) TARGET="$TUNNEL_TARGET" ;;
esac

# Build list of hostnames to update based on scope.
HOSTS=()
case "$SCOPE" in
  dev)  HOSTS=("dev.elkhair.tech") ;;
  prod) HOSTS=("elkhair.tech" "www.elkhair.tech") ;;
  both) HOSTS=("dev.elkhair.tech" "elkhair.tech" "www.elkhair.tech") ;;
  *) echo "Invalid scope: $SCOPE (expected dev|prod|both)" >&2; exit 2 ;;
esac

CF_API="https://api.cloudflare.com/client/v4"
AUTH=(-H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" -H "Content-Type: application/json")

upsert_cname() {
  local host="$1"
  local target="$2"

  echo "  → ${host} → ${target}"

  # Find the existing record id (if any) so we can PATCH in place.
  local existing
  existing=$(curl -s "${CF_API}/zones/${CLOUDFLARE_ZONE_ID_TECH}/dns_records?type=CNAME&name=${host}" "${AUTH[@]}")
  local record_id
  record_id=$(echo "$existing" | jq -r '.result[0].id // empty')

  local body
  body=$(jq -n --arg name "$host" --arg target "$target" \
    '{type:"CNAME", name:$name, content:$target, ttl:300, proxied:true}')

  if [[ -n "$record_id" ]]; then
    curl -fsS -X PATCH "${CF_API}/zones/${CLOUDFLARE_ZONE_ID_TECH}/dns_records/${record_id}" \
      "${AUTH[@]}" -d "$body" >/dev/null
  else
    curl -fsS -X POST "${CF_API}/zones/${CLOUDFLARE_ZONE_ID_TECH}/dns_records" \
      "${AUTH[@]}" -d "$body" >/dev/null
  fi
}

echo "Flipping landing to ${MODE^^} (${TARGET}) for scope=${SCOPE}"
for host in "${HOSTS[@]}"; do
  upsert_cname "$host" "$TARGET"
done

echo
echo "Done. DNS propagation inside Cloudflare is near-instant; external resolvers"
echo "may cache the old value for up to the previous TTL. Test with:"
for host in "${HOSTS[@]}"; do
  echo "    dig +short ${host} @1.1.1.1"
done
