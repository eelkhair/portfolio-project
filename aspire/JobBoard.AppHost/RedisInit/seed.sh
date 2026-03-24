#!/bin/sh
set -e

echo "Seeding Redis config keys..."

REDIS="redis-cli -h redis -p 6379 -n 1"

# ── Global config ─────────────────────────────────────────────────────
$REDIS SET "jobboard:config:global:FeatureFlags:Monolith" "false"
$REDIS SET "jobboard:config:global:FeatureFlags:DraftGeneration" "true"
$REDIS SET "jobboard:config:global:FeatureFlags:ResumeParser" "true"
$REDIS SET "jobboard:config:global:FeatureFlags:Chat" "true"
$REDIS SET "jobboard:config:global:SystemMode" "microservices"

# Service URLs (local Aspire ports)
$REDIS SET "jobboard:config:global:AIServiceUrl" "http://localhost:5200"
$REDIS SET "jobboard:config:global:AdminApiUrl" "http://localhost:5262"
$REDIS SET "jobboard:config:global:MonolithUrl" "http://localhost:5280"

# Elasticsearch
$REDIS SET "jobboard:config:global:ElasticConfiguration:Uri" "http://localhost:9200"

# SMTP (Mailpit)
$REDIS SET "jobboard:config:global:SMTP:Host" "localhost"
$REDIS SET "jobboard:config:global:SMTP:Port" "1025"

# Seq
$REDIS SET "jobboard:config:global:SeqServerUrl" "http://localhost:5341"

# OTel (no zipkin locally)
$REDIS SET "jobboard:config:global:OTEL_EXPORTER_ZIPKIN_ENDPOINT" ""

# Health checks UI
$REDIS SET "jobboard:config:global:HealthChecksUI:EvaluationTimeInSeconds" "10"
$REDIS SET "jobboard:config:global:HealthChecksUI:MinimumSecondsBetweenFailureNotifications" "15"
$REDIS SET "jobboard:config:global:HealthChecksUI:CustomStylesheet" "/css/healthchecks-custom.css"
$REDIS SET "jobboard:config:global:HealthChecksUI:HealthChecks:0:Uri" "health-results"

# ── Per-service config ────────────────────────────────────────────────
$REDIS SET "jobboard:config:monolith-api:placeholder" "50"
$REDIS SET "jobboard:config:monolith-api:HealthUrl" "http://localhost:3333"
$REDIS SET "jobboard:config:monolith-api:HealthChecksUI:HealthChecks:0:Name" "Monolith Api"

$REDIS SET "jobboard:config:admin-api:placeholder" "1"
$REDIS SET "jobboard:config:admin-api:HealthUrl" "http://localhost:3334"
$REDIS SET "jobboard:config:admin-api:McpServer:HealthUrl" "http://localhost:3334"

$REDIS SET "jobboard:config:company-api:placeholder" "1"
$REDIS SET "jobboard:config:job-api:placeholder" "1"
$REDIS SET "jobboard:config:user-api:placeholder" "1"
$REDIS SET "jobboard:config:connector-api:placeholder" "1"
$REDIS SET "jobboard:config:connector-api:HealthChecksUI:HealthChecks:0:Name" "Connector Api"
$REDIS SET "jobboard:config:reverse-connector-api:placeholder" "placeholder"

# AI service v2
$REDIS SET "jobboard:config:ai-service-v2:AIModel" "gpt-4.1-mini"
$REDIS SET "jobboard:config:ai-service-v2:AIProvider" "openai"
$REDIS SET "jobboard:config:ai-service-v2:ai-source" "ai-service"

echo "Redis seeded successfully with $(redis-cli -h redis -p 6379 -n 1 KEYS 'jobboard:config:*' | wc -l) keys."
