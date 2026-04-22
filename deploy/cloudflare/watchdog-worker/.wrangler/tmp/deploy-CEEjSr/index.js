var __defProp = Object.defineProperty;
var __name = (target, value) => __defProp(target, "name", { value, configurable: true });

// src/index.ts
var DEFAULT_STATE = {
  mode: "tunnel",
  consecutive_failures: 0,
  consecutive_successes: 0,
  last_flip: null
};
var STATE_KEY = "state";
async function loadState(env) {
  const raw = await env.WATCHDOG_STATE.get(STATE_KEY);
  if (!raw) return { ...DEFAULT_STATE };
  try {
    return { ...DEFAULT_STATE, ...JSON.parse(raw) };
  } catch {
    return { ...DEFAULT_STATE };
  }
}
__name(loadState, "loadState");
async function saveState(env, state) {
  await env.WATCHDOG_STATE.put(STATE_KEY, JSON.stringify(state));
}
__name(saveState, "saveState");
async function probePublic(env) {
  try {
    const res = await fetch(env.PROBE_URL, {
      method: "GET",
      // Skip Workers cache — always hit the origin so failover isn't masked by
      // stale CF edge responses.
      cf: { cacheTtl: 0, cacheEverything: false },
      // Let CF abort slow probes cleanly.
      signal: AbortSignal.timeout(1e4)
    });
    return res.ok;
  } catch (err) {
    console.warn("public probe failed:", err.message);
    return false;
  }
}
__name(probePublic, "probePublic");
async function probeTunnelDirect(env) {
  try {
    const url = `https://${env.CLOUDFLARE_TUNNEL_ID}.cfargotunnel.com/`;
    const res = await fetch(url, {
      headers: { Host: "elkhair.tech" },
      signal: AbortSignal.timeout(1e4)
    });
    return res.ok;
  } catch (err) {
    console.warn("tunnel-direct probe failed:", err.message);
    return false;
  }
}
__name(probeTunnelDirect, "probeTunnelDirect");
async function cfApi(env, path, init = {}) {
  return fetch(`https://api.cloudflare.com/client/v4${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${env.CLOUDFLARE_API_TOKEN}`,
      "Content-Type": "application/json",
      ...init.headers
    }
  });
}
__name(cfApi, "cfApi");
async function upsertCname(env, host, target) {
  const zone = env.CLOUDFLARE_ZONE_ID_TECH;
  const listRes = await cfApi(
    env,
    `/zones/${zone}/dns_records?type=CNAME&name=${encodeURIComponent(host)}`
  );
  const listJson = await listRes.json();
  const existing = listJson.result?.[0];
  const body = JSON.stringify({
    type: "CNAME",
    name: host,
    content: target,
    ttl: 300,
    proxied: true
  });
  if (existing) {
    const res = await cfApi(env, `/zones/${zone}/dns_records/${existing.id}`, {
      method: "PATCH",
      body
    });
    if (!res.ok) {
      throw new Error(
        `PATCH ${host}: HTTP ${res.status} ${await res.text()}`
      );
    }
    console.log(`  PATCH ${host} \u2192 ${target}`);
  } else {
    const res = await cfApi(env, `/zones/${zone}/dns_records`, {
      method: "POST",
      body
    });
    if (!res.ok) {
      throw new Error(`POST ${host}: HTTP ${res.status} ${await res.text()}`);
    }
    console.log(`  POST ${host} \u2192 ${target}`);
  }
}
__name(upsertCname, "upsertCname");
async function flipDns(env, target) {
  const targetHost = target === "pages" ? `${env.PAGES_PROJECT}.pages.dev` : `${env.CLOUDFLARE_TUNNEL_ID}.cfargotunnel.com`;
  const hosts = env.LANDING_HOSTS.split(",").map((s) => s.trim()).filter(Boolean);
  console.log(`flipping DNS \u2192 ${target} (${targetHost}) for ${hosts.join(", ")}`);
  for (const host of hosts) {
    await upsertCname(env, host, targetHost);
  }
}
__name(flipDns, "flipDns");
async function tick(env) {
  const state = await loadState(env);
  const probeOk = await probePublic(env);
  const failThreshold = parseInt(env.FAIL_THRESHOLD, 10);
  const successThreshold = parseInt(env.SUCCESS_THRESHOLD, 10);
  const hysteresisSecs = parseInt(env.HYSTERESIS_SECS, 10);
  console.log(
    `tick: mode=${state.mode} fails=${state.consecutive_failures} successes=${state.consecutive_successes} probe=${probeOk ? "OK" : "FAIL"}`
  );
  if (state.mode === "tunnel") {
    if (probeOk) {
      state.consecutive_failures = 0;
    } else {
      state.consecutive_failures += 1;
      state.consecutive_successes = 0;
    }
  } else {
    if (probeOk) {
      state.consecutive_successes += 1;
    } else {
      state.consecutive_successes = 0;
    }
  }
  if (state.mode === "tunnel" && state.consecutive_failures >= failThreshold) {
    console.log("FAILOVER: flipping to pages");
    await flipDns(env, "pages");
    state.mode = "pages";
    state.last_flip = (/* @__PURE__ */ new Date()).toISOString();
    state.consecutive_failures = 0;
    state.consecutive_successes = 0;
    await saveState(env, state);
    return state;
  }
  if (state.mode === "pages" && state.consecutive_successes >= successThreshold) {
    const lastFlipSecs = state.last_flip ? (Date.now() - new Date(state.last_flip).getTime()) / 1e3 : Infinity;
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
    state.last_flip = (/* @__PURE__ */ new Date()).toISOString();
    state.consecutive_failures = 0;
    state.consecutive_successes = 0;
    await saveState(env, state);
    return state;
  }
  await saveState(env, state);
  return state;
}
__name(tick, "tick");
var index_default = {
  async scheduled(_event, env, ctx) {
    ctx.waitUntil(tick(env));
  },
  async fetch(_req, env) {
    const state = await loadState(env);
    return Response.json(state, {
      headers: { "cache-control": "no-store" }
    });
  }
};
export {
  index_default as default
};
//# sourceMappingURL=index.js.map
