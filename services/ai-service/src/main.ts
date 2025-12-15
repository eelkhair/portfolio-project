import { loadSecretsIntoEnv } from "./dapr-secret-loader.js";
import { loadConfigIntoEnv } from "./dapr-config-loader.js";
import { loadConfig } from "./config.js";

const CONFIG_REFRESH_MS = Number(process.env.CONFIG_REFRESH_MS ?? 10_000);

async function main() {
    // Load secrets once (startup only)
    await loadSecretsIntoEnv();

    // Load config initially
    await loadConfigIntoEnv();

    // Periodically refresh config (safe keys only)
    setInterval(async () => {
        try {
            await loadConfigIntoEnv();
            console.log("[CONFIG] Configuration refreshed");

        } catch (err) {
            console.error("[CONFIG] Refresh failed:", err);
        }
    }, CONFIG_REFRESH_MS);

    const config = loadConfig();
    const { startServer } = await import("./server.js");
    await startServer(config);
}

main().catch((err) => {
    console.error("FATAL startup error:", err);
    process.exit(1);
});
