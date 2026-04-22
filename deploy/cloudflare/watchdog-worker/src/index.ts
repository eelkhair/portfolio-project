/**
 * Watchdog DNS-failover Worker
 *
 * Triggered by a 1-minute cron. Probes the public landing URL; tracks
 * consecutive fail/success counts in KV. On sustained failure, flips the
 * landing CNAMEs to Cloudflare Pages. On sustained recovery (AND a
 * tunnel-direct probe confirming cloudflared is reconnected) AND hysteresis,
 * flips them back to the tunnel.
 *
 * Also exposes a `fetch` handler that returns the current state as JSON —
 * handy for debugging or plugging into a Kuma HTTP monitor.
 */

export interface Env {
  // KV
  WATCHDOG_STATE: KVNamespace;

  // Secrets
  CLOUDFLARE_API_TOKEN: string;

  // Vars (wrangler.toml)
  PROBE_URL: string;
  CLOUDFLARE_ZONE_ID_TECH: string;
  CLOUDFLARE_TUNNEL_ID: string;
  PAGES_PROJECT: string;
  LANDING_HOSTS: string;
  FAIL_THRESHOLD: string;
  SUCCESS_THRESHOLD: string;
  HYSTERESIS_SECS: string;
}

type Mode = "tunnel" | "pages";

interface State {
  mode: Mode;
  consecutive_failures: number;
  consecutive_successes: number;
  last_flip: string | null; // ISO-8601
}

const DEFAULT_STATE: State = {
  mode: "tunnel",
  consecutive_failures: 0,
  consecutive_successes: 0,
  last_flip: null,
};

const STATE_KEY = "state";

// ── State I/O ────────────────────────────────────────────────────────────

async function loadState(env: Env): Promise<State> {
  const raw = await env.WATCHDOG_STATE.get(STATE_KEY);
  if (!raw) return { ...DEFAULT_STATE };
  try {
    return { ...DEFAULT_STATE, ...(JSON.parse(raw) as Partial<State>) };
  } catch {
    return { ...DEFAULT_STATE };
  }
}

async function saveState(env: Env, state: State): Promise<void> {
  await env.WATCHDOG_STATE.put(STATE_KEY, JSON.stringify(state));
}

// ── Probes ───────────────────────────────────────────────────────────────

async function probePublic(env: Env): Promise<boolean> {
  try {
    const res = await fetch(env.PROBE_URL, {
      method: "GET",
      // Skip Workers cache — always hit the origin so failover isn't masked by
      // stale CF edge responses.
      cf: { cacheTtl: 0, cacheEverything: false },
      // Let CF abort slow probes cleanly.
      signal: AbortSignal.timeout(10_000),
    });
    return res.ok;
  } catch (err) {
    console.warn("public probe failed:", (err as Error).message);
    return false;
  }
}

/**
 * Bypasses DNS and hits the Cloudflare tunnel's origin URL directly. If the
 * tunnel's cloudflared is disconnected, CF edge returns 530 / 502, which
 * gives us a ground-truth signal for "is it safe to flip DNS back?" —
 * independent of the public URL's current state (which is Pages during
 * failover).
 */
async function probeTunnelDirect(env: Env): Promise<boolean> {
  try {
    const url = `https://${env.CLOUDFLARE_TUNNEL_ID}.cfargotunnel.com/`;
    const res = await fetch(url, {
      headers: { Host: "elkhair.tech" },
      signal: AbortSignal.timeout(10_000),
    });
    return res.ok;
  } catch (err) {
    console.warn("tunnel-direct probe failed:", (err as Error).message);
    return false;
  }
}

// ── DNS flip ─────────────────────────────────────────────────────────────

interface CfRecord {
  id: string;
  name: string;
  content: string;
  type: string;
}

async function cfApi(
  env: Env,
  path: string,
  init: RequestInit = {}
): Promise<Response> {
  return fetch(`https://api.cloudflare.com/client/v4${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${env.CLOUDFLARE_API_TOKEN}`,
      "Content-Type": "application/json",
      ...(init.headers as Record<string, string> | undefined),
    },
  });
}

async function upsertCname(
  env: Env,
  host: string,
  target: string
): Promise<void> {
  const zone = env.CLOUDFLARE_ZONE_ID_TECH;
  const listRes = await cfApi(
    env,
    `/zones/${zone}/dns_records?type=CNAME&name=${encodeURIComponent(host)}`
  );
  const listJson = (await listRes.json()) as { result?: CfRecord[] };
  const existing = listJson.result?.[0];

  const body = JSON.stringify({
    type: "CNAME",
    name: host,
    content: target,
    ttl: 300,
    proxied: true,
  });

  if (existing) {
    const res = await cfApi(env, `/zones/${zone}/dns_records/${existing.id}`, {
      method: "PATCH",
      body,
    });
    if (!res.ok) {
      throw new Error(
        `PATCH ${host}: HTTP ${res.status} ${await res.text()}`
      );
    }
    console.log(`  PATCH ${host} → ${target}`);
  } else {
    const res = await cfApi(env, `/zones/${zone}/dns_records`, {
      method: "POST",
      body,
    });
    if (!res.ok) {
      throw new Error(`POST ${host}: HTTP ${res.status} ${await res.text()}`);
    }
    console.log(`  POST ${host} → ${target}`);
  }
}

async function flipDns(env: Env, target: Mode): Promise<void> {
  const targetHost =
    target === "pages"
      ? `${env.PAGES_PROJECT}.pages.dev`
      : `${env.CLOUDFLARE_TUNNEL_ID}.cfargotunnel.com`;
  const hosts = env.LANDING_HOSTS.split(",").map((s) => s.trim()).filter(Boolean);

  console.log(`flipping DNS → ${target} (${targetHost}) for ${hosts.join(", ")}`);
  for (const host of hosts) {
    await upsertCname(env, host, targetHost);
  }
}

// ── State machine ────────────────────────────────────────────────────────

async function tick(env: Env): Promise<State> {
  const state = await loadState(env);
  const probeOk = await probePublic(env);
  const failThreshold = parseInt(env.FAIL_THRESHOLD, 10);
  const successThreshold = parseInt(env.SUCCESS_THRESHOLD, 10);
  const hysteresisSecs = parseInt(env.HYSTERESIS_SECS, 10);

  console.log(
    `tick: mode=${state.mode} fails=${state.consecutive_failures} ` +
      `successes=${state.consecutive_successes} probe=${probeOk ? "OK" : "FAIL"}`
  );

  // Update counters.
  if (state.mode === "tunnel") {
    if (probeOk) {
      state.consecutive_failures = 0;
    } else {
      state.consecutive_failures += 1;
      state.consecutive_successes = 0;
    }
  } else {
    // mode === "pages" — we care about consecutive successes for flip-back.
    if (probeOk) {
      state.consecutive_successes += 1;
    } else {
      // Pages itself broken — rare; reset the recovery counter so a future
      // blip doesn't prematurely trip a flip-back.
      state.consecutive_successes = 0;
    }
  }

  // Failover: tunnel → pages.
  if (state.mode === "tunnel" && state.consecutive_failures >= failThreshold) {
    console.log("FAILOVER: flipping to pages");
    await flipDns(env, "pages");
    state.mode = "pages";
    state.last_flip = new Date().toISOString();
    state.consecutive_failures = 0;
    state.consecutive_successes = 0;
    await saveState(env, state);
    return state;
  }

  // Recovery: pages → tunnel. Gated by successes, hysteresis, AND a live
  // tunnel-direct probe (so we don't flip back while the tunnel is still dead).
  if (state.mode === "pages" && state.consecutive_successes >= successThreshold) {
    const lastFlipSecs = state.last_flip
      ? (Date.now() - new Date(state.last_flip).getTime()) / 1000
      : Infinity;
    if (lastFlipSecs < hysteresisSecs) {
      console.log(
        `recovery pending: successes met but within hysteresis (${Math.round(
          lastFlipSecs
        )}s < ${hysteresisSecs}s)`
      );
      await saveState(env, state);
      return state;
    }
    const tunnelOk = await probeTunnelDirect(env);
    if (!tunnelOk) {
      console.log("recovery pending: tunnel-direct probe failed, waiting");
      await saveState(env, state);
      return state;
    }
    console.log("RECOVERY: flipping back to tunnel");
    await flipDns(env, "tunnel");
    state.mode = "tunnel";
    state.last_flip = new Date().toISOString();
    state.consecutive_failures = 0;
    state.consecutive_successes = 0;
    await saveState(env, state);
    return state;
  }

  await saveState(env, state);
  return state;
}

// ── Worker entry points ──────────────────────────────────────────────────

export default {
  async scheduled(
    _event: ScheduledController,
    env: Env,
    ctx: ExecutionContext
  ): Promise<void> {
    ctx.waitUntil(tick(env));
  },

  async fetch(_req: Request, env: Env): Promise<Response> {
    // Return the current state as JSON — non-sensitive, useful for wiring
    // into a Kuma HTTP monitor or just eyeballing from a browser. Manual
    // triggers are done via `wrangler` tooling or the CF dashboard's cron
    // "Trigger" button, not an HTTP endpoint (avoids exposing a DNS-flipping
    // action behind a header check).
    const state = await loadState(env);
    return Response.json(state, {
      headers: { "cache-control": "no-store" },
    });
  },
};
