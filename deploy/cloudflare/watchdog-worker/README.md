# watchdog-dns Worker

Cloudflare Worker that replaces `.github/workflows/watchdog-dns.yml`. Runs
on a **1-minute cron trigger** (reliable to the minute, unlike GH Actions
which drifted to ~30 min in practice). Probes `https://elkhair.tech/` from
the Cloudflare edge; on sustained failure, flips the landing CNAMEs to the
Pages backup. On sustained recovery, flips them back.

## Behavior (same state machine as the old GH Action)

| Event | Trigger | Wall-clock |
|---|---|---|
| Flip to Pages | 2 consecutive failed probes | ~2 min after outage |
| Flip back to tunnel | 6 consecutive greens + live tunnel-direct probe + 15 min hysteresis | ~6 min after recovery |

Tuning lives in `wrangler.toml` as vars (`FAIL_THRESHOLD`,
`SUCCESS_THRESHOLD`, `HYSTERESIS_SECS`).

## Files

```
wrangler.toml       # cron, vars, KV binding
src/index.ts        # probe + state machine + DNS API calls
package.json        # wrangler + workers-types
tsconfig.json       # strict TS for the worker
```

State lives in a KV namespace (key `state`, JSON blob):

```json
{
  "mode": "tunnel" | "pages",
  "consecutive_failures": 0,
  "consecutive_successes": 0,
  "last_flip": "2026-04-22T14:30:00Z" | null
}
```

## One-time setup

```bash
cd deploy/cloudflare/watchdog-worker
npm install

# 1. Auth
npx wrangler login

# 2. Create the KV namespace and paste the returned ID into wrangler.toml
npx wrangler kv namespace create WATCHDOG_STATE

# 3. Fill in the two REPLACE_WITH_* placeholders in wrangler.toml:
#      CLOUDFLARE_ZONE_ID_TECH  (elkhair.tech zone id)
#      CLOUDFLARE_TUNNEL_ID     (the cfargotunnel.com uuid)
#    plus the kv_namespaces[0].id from step 2.

# 4. Secret: Cloudflare API token scoped to Zone:DNS:Edit on elkhair.tech.
#    This is the same token used by deploy/cloudflare/failover-landing.sh;
#    paste it when prompted.
npx wrangler secret put CLOUDFLARE_API_TOKEN

# 5. Deploy
npx wrangler deploy
```

## Verify

```bash
# Worker URL prints current state as JSON (non-sensitive — safe to hit from anywhere).
curl https://watchdog-dns.<your-cf-account>.workers.dev/

# Live logs from the most recent cron invocations
npx wrangler tail

# Manual trigger: Cloudflare dashboard → Workers & Pages → watchdog-dns →
# Triggers → your cron entry → "Trigger now". Or `wrangler triggers deploy`
# then invoke via dashboard.
```

You should see a log line every minute:

```
tick: mode=tunnel fails=0 successes=0 probe=OK
```

## Drill

To prove the failover path without breaking prod, temporarily edit
`wrangler.toml` so `PROBE_URL` points at a host that always 500s, bump
`FAIL_THRESHOLD` to `1`, and `npx wrangler deploy`. Watch one tick — should
log `FAILOVER: flipping to pages` and the landing CNAMEs should now resolve
to `landing.pages.dev`. Revert the vars, redeploy, and watch it flip back
(respecting hysteresis).

## Relationship to the LAN watchdog

This Worker and the LAN watchdog (`deploy/watchdog/`) are deliberately
uncoordinated. The LAN watchdog tries to *fix* prod (docker restart → VM
reboot → free cluster RAM). This Worker routes around a dead prod. In a
typical outage the LAN watchdog recovers things before the Worker's 2-tick
failure threshold, and nothing flips. In a whole-cluster outage the Worker
is the only layer that can respond.

## Cost

Free tier:
- 1,440 cron invocations/day vs. 100,000 limit
- ~3 subrequests/invocation vs. 50 limit
- ~1 KV write/day (only on state change) vs. 1,000 limit

Stays free indefinitely at this cadence.

## Retiring the old GH Action path

`.github/workflows/watchdog-dns.yml`, `.github/scripts/watchdog-probe.sh`,
and `.github/watchdog-state.json` are retired. The guard in
`.github/workflows/cloudflare-tunnel.yml` that used to read
`watchdog-state.json` now queries the live CNAME targets instead — no
cross-file coupling.
