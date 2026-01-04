import fs from "fs/promises";
import path from "path";

const SECRETSTORE = process.env.SECRETSTORE_NAME || "vault";
const GROUP_KEY = "ai"; // The bundle key in Vault

export async function loadSecretsIntoEnv() {
    console.log("[BOOT] Loading grouped secret bundle from Dapr/Vault...");

    const daprPort = process.env.DAPR_HTTP_PORT || "3500";

    const url = `http://127.0.0.1:${daprPort}/v1.0/secrets/${SECRETSTORE}/${GROUP_KEY}`;

    let json: any;

    try {
        const res = await fetch(url);
        json = await res.json();

        if (!res.ok) {
            console.error(`[DAPR] Error fetching secret bundle '${GROUP_KEY}':`, json);
            return;
        }
    } catch (err: any) {
        console.error("[DAPR] FETCH ERROR:", err);
        throw err;
    }

    const keys = Object.keys(json);
    console.log(`[DAPR] Loaded secret bundle '${GROUP_KEY}' containing:`, keys);

    for (const key of keys) {
        process.env[key] = json[key];
    }
}
