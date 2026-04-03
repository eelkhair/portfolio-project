#!/bin/bash
echo "=== Seed Runner: starting ==="

/seeds/seed-redis.sh &
/seeds/seed-sqlserver.sh &
/seeds/seed-postgres.sh &
wait

echo "=== All seeds complete. Health endpoint ready on :8080 ==="

# HTTP health endpoint — Aspire probes this before starting dependent services
while true; do
    echo -e "HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nOK" | nc -l -p 8080 -w 1 2>/dev/null || true
done
