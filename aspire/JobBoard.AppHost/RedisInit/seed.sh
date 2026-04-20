#!/bin/sh

echo "Seeding Redis config keys..."

REDIS="redis-cli -h redis -p 6379 -n 1"

# ── Global config (SET NX = only seed if key doesn't exist, preserves runtime changes) ──
$REDIS SET "jobboard:config:global:FeatureFlags:Monolith" "false" NX
$REDIS SET "jobboard:config:global:FeatureFlags:PublicChat" "true" NX
$REDIS SET "jobboard:config:global:FeatureFlags:DeepDives" "false" NX
$REDIS SET "jobboard:config:global:FeatureFlags:ContactForm" "false" NX
$REDIS SET "jobboard:config:global:FeatureFlags:AvailableBadge" "false" NX
$REDIS SET "jobboard:config:global:FeatureFlags:ServiceStatus" "false" NX


# SMTP (Mailpit)
$REDIS SET "jobboard:config:global:SMTP:Host" "localhost"
$REDIS SET "jobboard:config:global:SMTP:Port" "1025"

# OTel (no zipkin locally)
$REDIS SET "jobboard:config:global:OTEL_EXPORTER_ZIPKIN_ENDPOINT" ""

# Health checks UI
$REDIS SET "jobboard:config:global:HealthChecksUI:EvaluationTimeInSeconds" "10"
$REDIS SET "jobboard:config:global:HealthChecksUI:MinimumSecondsBetweenFailureNotifications" "15"
$REDIS SET "jobboard:config:global:HealthChecksUI:CustomStylesheet" "/css/healthchecks-custom.css"
$REDIS SET "jobboard:config:global:HealthChecksUI:HealthChecks:0:Uri" "health-results"

# ── Per-service config ────────────────────────────────────────────────
$REDIS SET "jobboard:config:monolith-api:placeholder" "50"
$REDIS SET "jobboard:config:monolith-api:HealthChecksUI:HealthChecks:0:Name" "Monolith Api"

$REDIS SET "jobboard:config:admin-api:placeholder" "1"

$REDIS SET "jobboard:config:company-api:placeholder" "1"
$REDIS SET "jobboard:config:job-api:placeholder" "1"
$REDIS SET "jobboard:config:user-api:placeholder" "1"
$REDIS SET "jobboard:config:connector-api:placeholder" "1"
$REDIS SET "jobboard:config:connector-api:HealthChecksUI:HealthChecks:0:Name" "Connector Api"
$REDIS SET "jobboard:config:reverse-connector-api:placeholder" "placeholder"

# AI service v2 (NX = preserve runtime changes to provider/model)
$REDIS SET "jobboard:config:ai-service-v2:AIModel" "gpt-4.1" NX
$REDIS SET "jobboard:config:ai-service-v2:AIProvider" "openai" NX
$REDIS SET "jobboard:config:ai-service-v2:ai-source" "ai-service" NX

echo "Redis seeded successfully with $(redis-cli -h redis -p 6379 -n 1 KEYS 'jobboard:config:*' | wc -l) keys."
