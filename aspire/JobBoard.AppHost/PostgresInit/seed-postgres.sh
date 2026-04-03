#!/bin/bash
set -e

HOST="postgres"
PORT=5432
USER="postgres"
DB="AiEmbeddings"
export PGPASSWORD="postgres"

echo "=== PostgreSQL Seed: waiting for PostgreSQL to accept connections... ==="
for i in $(seq 1 30); do
  pg_isready -h $HOST -p $PORT -U $USER > /dev/null 2>&1 && break
  echo "  Attempt $i/30..."
  sleep 2
done

if ! pg_isready -h $HOST -p $PORT -U $USER > /dev/null 2>&1; then
  echo "ERROR: Could not connect to PostgreSQL after 30 attempts."
  exit 1
fi

echo "Connected to PostgreSQL."

# Check if the database has user tables (excluding migration history)
TABLE_COUNT=$(psql -h $HOST -p $PORT -U $USER -d $DB -t -c "
  SELECT COUNT(*) FROM information_schema.tables
  WHERE table_schema = 'public'
    AND table_type = 'BASE TABLE'
    AND table_name != '__EFMigrationsHistory'
" 2>/dev/null | tr -d '[:space:]')

if [ "$TABLE_COUNT" -gt 0 ] 2>/dev/null; then
  echo "  ${DB}: already has ${TABLE_COUNT} user tables — skipping restore."
else
  echo "  ${DB}: restoring from backup..."
  pg_restore -h $HOST -p $PORT -U $USER -d $DB \
    --no-owner --no-privileges --clean --if-exists \
    /seed-backups/AiEmbeddings.dump
  echo "  ${DB}: restore complete."
fi

echo ""
echo "=== PostgreSQL seed complete. ==="
