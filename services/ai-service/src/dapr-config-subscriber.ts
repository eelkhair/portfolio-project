import { context } from "@opentelemetry/api";
import { suppressTracing } from "@opentelemetry/core";

const DAPR_PORT = process.env.DAPR_HTTP_PORT ?? "3500";
const DAPR_BASE = `http://127.0.0.1:${DAPR_PORT}/v1.0`;

type DaprConfigChange = {
    items?: Record<
        string,
        {
            value?: string;
            version?: string;
            metadata?: Record<string, string>;
        }
    >;
};

export async function subscribeToConfigChanges(
    store: string,
    keys: string[],
    onChange: (key: string, value: string | undefined) => void
) {
    const keyQuery = keys.length > 0 ? `?keys=${keys.join(",")}` : "";
    const url = `${DAPR_BASE}/configuration/${store}${keyQuery}`;

    const res = await fetch(url, {
        method: "GET",
        headers: {
            Accept: "application/json",
        },
    });

    if (!res.ok || !res.body) {
        throw new Error(
            `Failed to subscribe to config store '${store}' (${res.status})`
        );
    }

    console.log(`[DAPR] Subscribed to config changes on '${store}'`);

    const reader = res.body.getReader();
    const decoder = new TextDecoder();

    while (true) {

        const { value, done } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value, { stream: true }).trim();
        if (!chunk) continue;

        let event: DaprConfigChange;
        try {
            event = JSON.parse(chunk);
        } catch {
            continue; // ignore partial chunks
        }

        for (const [fullKey, entry] of Object.entries(event.items ?? {})) {
            const key = normalizeConfigKey(fullKey);
            onChange(key, entry?.value);
        }
    }
}

function normalizeConfigKey(key: string): string {
    // jobboard:config:ai-service:FEATURE_X → FEATURE_X
    const parts = key.split(":");
    return parts[parts.length - 1];
}
