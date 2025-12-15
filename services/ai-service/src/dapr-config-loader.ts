const DAPR_PORT = process.env.DAPR_HTTP_PORT ?? "3500";
const DAPR_BASE = `http://127.0.0.1:${DAPR_PORT}/v1.0`;

const DEFAULT_CONFIG_STORES = ["appconfig-global"];

const SERVICE_NAME = process.env.DAPR_APP_ID ?? "ai-service";
const SERVICE_CONFIG_STORE =
    process.env.CONFIGURATION_STORE_NAME ?? `appconfig-${SERVICE_NAME}`;

type DaprConfigEntry = {
    value?: string;
};

// Tracks keys last loaded from Dapr
let lastLoadedKeys = new Set<string>();

export async function loadConfigIntoEnv() {
    const stores = await resolveConfigStores();

    const currentKeys = new Set<string>();

    for (const store of stores) {
        const res = await fetch(`${DAPR_BASE}/configuration/${store}`);
        if (!res.ok) {
            throw new Error(`Failed to load config store '${store}'`);
        }

        const json = (await res.json()) as Record<string, DaprConfigEntry>;

        for (const [fullKey, entry] of Object.entries(json)) {
            if (entry?.value === undefined) continue;

            const envKey = normalizeConfigKey(fullKey);

            currentKeys.add(envKey);
            process.env[envKey] = entry.value;
        }
    }

    // Remove keys that disappeared
    for (const key of lastLoadedKeys) {
        if (!currentKeys.has(key)) {
            delete process.env[key];
            console.log(`[CONFIG] Removed key '${key}'`);
        }
    }

    lastLoadedKeys = currentKeys;
}

async function resolveConfigStores(): Promise<string[]> {
    try {
        const res = await fetch(
            `${DAPR_BASE}/configuration/${SERVICE_CONFIG_STORE}`
        );

        if (!res.ok) return DEFAULT_CONFIG_STORES;

        const json = (await res.json()) as Record<string, DaprConfigEntry>;

        const match = Object.entries(json).find(([k]) =>
            k.endsWith(":CONFIGURATION_STORES")
        );

        const value = match?.[1]?.value;

        return typeof value === "string"
            ? value.split(",").map(s => s.trim()).filter(Boolean)
            : DEFAULT_CONFIG_STORES;
    } catch {
        return DEFAULT_CONFIG_STORES;
    }
}

function normalizeConfigKey(key: string): string {
    const parts = key.split(":");
    return parts[parts.length - 1];
}
