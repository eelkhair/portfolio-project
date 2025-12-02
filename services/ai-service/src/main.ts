import {loadSecretsIntoEnv} from "./dapr-loader.js";
import {loadConfig} from "./config.js";


async function main() {
    await loadSecretsIntoEnv();

    const config = loadConfig();
    console.log("[BOOT] Configuration validated.");
console.log(config);
    const { startServer } = await import("./server.js");
    await startServer(config);
}

main().catch((err) => {
    console.error("FATAL startup error:", err);
    process.exit(1);
});
