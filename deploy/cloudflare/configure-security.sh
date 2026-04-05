#!/bin/bash
# ============================================================================
# Configure Cloudflare security settings for eelkhair.net
#
# Disables aggressive security features that block legitimate traffic
# to the portfolio homelab (Bot Fight Mode, Browser Integrity Check, etc.)
#
# Usage:
#   export CLOUDFLARE_API_TOKEN="your-api-token"
#   export CLOUDFLARE_ZONE_ID="your-zone-id"
#   ./configure-security.sh
# ============================================================================

set -euo pipefail

CF_API_BASE="https://api.cloudflare.com/client/v4"

# Validate required env vars
if [ -z "${CLOUDFLARE_API_TOKEN:-}" ]; then
  echo "Error: CLOUDFLARE_API_TOKEN is not set"
  exit 1
fi
if [ -z "${CLOUDFLARE_ZONE_ID:-}" ]; then
  echo "Error: CLOUDFLARE_ZONE_ID is not set"
  exit 1
fi

ZONE_ID="$CLOUDFLARE_ZONE_ID"
AUTH_HEADER="Authorization: Bearer $CLOUDFLARE_API_TOKEN"

echo "Configuring Cloudflare security for zone: $ZONE_ID"
echo "══════════════════════════════════════════════"

# ──────────────────────────────────────────────────────────────────────
# 1. Set Security Level to "essentially_off"
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "[1/4] Setting Security Level to 'essentially_off'..."
RESPONSE=$(curl -s -X PATCH \
  "$CF_API_BASE/zones/$ZONE_ID/settings/security_level" \
  -H "$AUTH_HEADER" \
  -H "Content-Type: application/json" \
  -d '{"value": "essentially_off"}')

SUCCESS=$(echo "$RESPONSE" | jq -r '.success')
if [ "$SUCCESS" = "true" ]; then
  echo "  Done: Security Level set to essentially_off"
else
  echo "  Warning: $(echo "$RESPONSE" | jq -r '.errors[0].message // "Unknown error"')"
fi

# ──────────────────────────────────────────────────────────────────────
# 2. Disable Bot Fight Mode
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "[2/4] Disabling Bot Fight Mode..."
RESPONSE=$(curl -s -X PUT \
  "$CF_API_BASE/zones/$ZONE_ID/bot_management" \
  -H "$AUTH_HEADER" \
  -H "Content-Type: application/json" \
  -d '{"fight_mode": false, "enable_js": false}')

SUCCESS=$(echo "$RESPONSE" | jq -r '.success')
if [ "$SUCCESS" = "true" ]; then
  echo "  Done: Bot Fight Mode disabled"
else
  echo "  Warning: $(echo "$RESPONSE" | jq -r '.errors[0].message // "Unknown error"')"
fi

# ──────────────────────────────────────────────────────────────────────
# 3. Disable Browser Integrity Check
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "[3/4] Disabling Browser Integrity Check..."
RESPONSE=$(curl -s -X PATCH \
  "$CF_API_BASE/zones/$ZONE_ID/settings/browser_check" \
  -H "$AUTH_HEADER" \
  -H "Content-Type: application/json" \
  -d '{"value": "off"}')

SUCCESS=$(echo "$RESPONSE" | jq -r '.success')
if [ "$SUCCESS" = "true" ]; then
  echo "  Done: Browser Integrity Check disabled"
else
  echo "  Warning: $(echo "$RESPONSE" | jq -r '.errors[0].message // "Unknown error"')"
fi

# ──────────────────────────────────────────────────────────────────────
# 4. Disable Email Address Obfuscation (can interfere with SPAs)
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "[4/4] Disabling Email Address Obfuscation..."
RESPONSE=$(curl -s -X PATCH \
  "$CF_API_BASE/zones/$ZONE_ID/settings/email_obfuscation" \
  -H "$AUTH_HEADER" \
  -H "Content-Type: application/json" \
  -d '{"value": "off"}')

SUCCESS=$(echo "$RESPONSE" | jq -r '.success')
if [ "$SUCCESS" = "true" ]; then
  echo "  Done: Email Obfuscation disabled"
else
  echo "  Warning: $(echo "$RESPONSE" | jq -r '.errors[0].message // "Unknown error"')"
fi

# ──────────────────────────────────────────────────────────────────────
# Verify current settings
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════"
echo "Verifying settings..."
echo ""

for SETTING in security_level browser_check email_obfuscation; do
  VALUE=$(curl -s "$CF_API_BASE/zones/$ZONE_ID/settings/$SETTING" \
    -H "$AUTH_HEADER" | jq -r '.result.value')
  echo "  $SETTING = $VALUE"
done

BOT_MODE=$(curl -s "$CF_API_BASE/zones/$ZONE_ID/bot_management" \
  -H "$AUTH_HEADER" | jq -r '.result.fight_mode')
echo "  bot_fight_mode = $BOT_MODE"

echo ""
echo "══════════════════════════════════════════════"
echo "Security configuration complete."
echo "Try accessing your site again from your phone."
echo "══════════════════════════════════════════════"
