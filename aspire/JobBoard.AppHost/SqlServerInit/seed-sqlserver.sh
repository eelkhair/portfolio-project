#!/bin/bash
set -e

SERVER="sqlserver"
PORT=1433
USER="sa"
PASS='YourStrong!Passw0rd'

if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then
  SQLCMD_BIN="/opt/mssql-tools18/bin/sqlcmd"
elif [ -x /opt/mssql-tools/bin/sqlcmd ]; then
  SQLCMD_BIN="/opt/mssql-tools/bin/sqlcmd"
else
  echo "ERROR: sqlcmd not found"; exit 1
fi

SQLCMD="$SQLCMD_BIN -S ${SERVER},${PORT} -U $USER -P $PASS -C -b"

echo "=== SQL Server Seed: waiting for SQL Server to accept connections... ==="
for i in $(seq 1 30); do
  $SQLCMD -Q "SELECT 1" > /dev/null 2>&1 && break
  echo "  Attempt $i/30..."
  sleep 2
done

if ! $SQLCMD -Q "SELECT 1" > /dev/null 2>&1; then
  echo "ERROR: Could not connect to SQL Server after 30 attempts."
  exit 1
fi

echo "Connected to SQL Server."

# ---------------------------------------------------------------------------
# Restore job-board-monolith
# ---------------------------------------------------------------------------
restore_db() {
  local DB_NAME="$1"
  local BAK_FILE="$2"
  local DATA_LOGICAL="$3"
  local LOG_LOGICAL="$4"
  local MDF_PATH="$5"
  local LDF_PATH="$6"

  TABLE_COUNT=$($SQLCMD -h -1 -Q "
    IF DB_ID('${DB_NAME}') IS NOT NULL
      SELECT COUNT(*) FROM [${DB_NAME}].sys.tables
        WHERE type = 'U' AND name != '__EFMigrationsHistory'
    ELSE
      SELECT 0
  " 2>/dev/null | tr -d '[:space:]')

  if [ "$TABLE_COUNT" -gt 0 ] 2>/dev/null; then
    echo "  ${DB_NAME}: already has ${TABLE_COUNT} user tables — skipping restore."
    return 0
  fi

  echo "  ${DB_NAME}: restoring from ${BAK_FILE}..."

  # Kill any existing connections before restoring
  $SQLCMD -Q "
    IF DB_ID('${DB_NAME}') IS NOT NULL
      ALTER DATABASE [${DB_NAME}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  " 2>/dev/null || true

  $SQLCMD -Q "
    RESTORE DATABASE [${DB_NAME}]
    FROM DISK = '${BAK_FILE}'
    WITH MOVE '${DATA_LOGICAL}' TO '${MDF_PATH}',
         MOVE '${LOG_LOGICAL}'  TO '${LDF_PATH}',
         REPLACE
  "

  # Restore multi-user access
  $SQLCMD -Q "ALTER DATABASE [${DB_NAME}] SET MULTI_USER;" 2>/dev/null || true

  echo "  ${DB_NAME}: restore complete."
}

echo ""
echo "--- Checking job-board-monolith ---"
restore_db \
  "job-board-monolith" \
  "/seed-backups/job-board-monolith.bak" \
  "job-board-monolith_Data" \
  "job-board-monolith_Log" \
  "/var/opt/mssql/data/job-board-monolith.mdf" \
  "/var/opt/mssql/data/job-board-monolith_log.ldf"

echo ""
echo "--- Checking job-board ---"
restore_db \
  "job-board" \
  "/seed-backups/job-board.bak" \
  "job-board_Data" \
  "job-board_Log" \
  "/var/opt/mssql/data/job-board.mdf" \
  "/var/opt/mssql/data/job-board_log.ldf"

echo ""
echo "=== SQL Server seed complete. ==="
