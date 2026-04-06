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
ZONE_ID="${CLOUDFLARE_ZONE_ID:-fd8cabfeb7708e503bdacccb209fe126}"
AUTH_HEADER="Authorization: Bearer $CLOUDFLARE_API_TOKEN"

echo "Configuring Cloudflare security for zone: $ZONE_ID"
echo "══════════════════════════════════════════════"

# ──────────────────────────────────────────────────────────────────────
# 1. Set Security Level to "essentially_off"
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "[1/7] Setting Security Level to 'essentially_off'..."
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
echo "[2/7] Disabling Bot Fight Mode..."
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
echo "[3/7] Disabling Browser Integrity Check..."
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
echo "[4/7] Disabling Email Address Obfuscation..."
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
# 5. Block Keycloak admin console publicly
#    Allows OIDC endpoints (/realms/...) but blocks /admin and /master
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "[5/7] Creating WAF rule to block Keycloak admin console..."

# Check if rule already exists
EXISTING_RULE=$(curl -s \
  "$CF_API_BASE/zones/$ZONE_ID/rulesets" \
  -H "$AUTH_HEADER" \
  | jq -r '.result[] | select(.phase == "http_request_firewall_custom") | .id')

if [ -n "$EXISTING_RULE" ]; then
  # Get existing rules to avoid overwriting them
  EXISTING_RULES=$(curl -s \
    "$CF_API_BASE/zones/$ZONE_ID/rulesets/$EXISTING_RULE" \
    -H "$AUTH_HEADER")

  # Check if our rule already exists
  HAS_RULE=$(echo "$EXISTING_RULES" | jq '[.result.rules[] | select(.description == "Block Keycloak admin console")] | length')

  if [ "$HAS_RULE" -gt 0 ]; then
    echo "  Rule already exists, skipping"
  else
    # Add our rule to the existing ruleset
    RESPONSE=$(curl -s -X POST \
      "$CF_API_BASE/zones/$ZONE_ID/rulesets/$EXISTING_RULE/rules" \
      -H "$AUTH_HEADER" \
      -H "Content-Type: application/json" \
      -d '{
        "description": "Block Keycloak admin console",
        "expression": "(http.host eq \"auth.eelkhair.net\" and starts_with(http.request.uri.path, \"/admin\"))",
        "action": "block"
      }')

    SUCCESS=$(echo "$RESPONSE" | jq -r '.success')
    if [ "$SUCCESS" = "true" ]; then
      echo "  Done: Keycloak /admin blocked publicly"
    else
      echo "  Warning: $(echo "$RESPONSE" | jq -r '.errors[0].message // "Unknown error"')"
    fi
  fi
else
  # Create new custom ruleset with our rule
  RESPONSE=$(curl -s -X POST \
    "$CF_API_BASE/zones/$ZONE_ID/rulesets" \
    -H "$AUTH_HEADER" \
    -H "Content-Type: application/json" \
    -d '{
      "name": "Portfolio security rules",
      "kind": "zone",
      "phase": "http_request_firewall_custom",
      "rules": [
        {
          "description": "Block Keycloak admin console",
          "expression": "(http.host eq \"auth.eelkhair.net\" and starts_with(http.request.uri.path, \"/admin\"))",
          "action": "block"
        }
      ]
    }')

  SUCCESS=$(echo "$RESPONSE" | jq -r '.success')
  if [ "$SUCCESS" = "true" ]; then
    echo "  Done: Keycloak /admin blocked publicly"
  else
    echo "  Warning: $(echo "$RESPONSE" | jq -r '.errors[0].message // "Unknown error"')"
  fi
fi

# ──────────────────────────────────────────────────────────────────────
# 6. Enable Always Use HTTPS
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "[6/7] Enabling Always Use HTTPS..."
RESPONSE=$(curl -s -X PATCH \
  "$CF_API_BASE/zones/$ZONE_ID/settings/always_use_https" \
  -H "$AUTH_HEADER" \
  -H "Content-Type: application/json" \
  -d '{"value": "on"}')

SUCCESS=$(echo "$RESPONSE" | jq -r '.success')
if [ "$SUCCESS" = "true" ]; then
  echo "  Done: Always Use HTTPS enabled"
else
  echo "  Warning: $(echo "$RESPONSE" | jq -r '.errors[0].message // "Unknown error"')"
fi

# ──────────────────────────────────────────────────────────────────────
# 7. Enable HSTS (Strict-Transport-Security)
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "[7/7] Enabling HSTS..."
RESPONSE=$(curl -s -X PATCH \
  "$CF_API_BASE/zones/$ZONE_ID/settings/security_header" \
  -H "$AUTH_HEADER" \
  -H "Content-Type: application/json" \
  -d '{
    "value": {
      "strict_transport_security": {
        "enabled": true,
        "max_age": 31536000,
        "include_subdomains": true,
        "nosniff": true
      }
    }
  }')

SUCCESS=$(echo "$RESPONSE" | jq -r '.success')
if [ "$SUCCESS" = "true" ]; then
  echo "  Done: HSTS enabled (max-age=31536000, includeSubDomains, nosniff)"
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

for SETTING in security_level browser_check email_obfuscation always_use_https; do
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
