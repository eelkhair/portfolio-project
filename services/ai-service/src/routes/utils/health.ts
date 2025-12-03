import type { FastifyInstance } from 'fastify';
import { performance } from 'node:perf_hooks';
import { context } from '@opentelemetry/api';
import { suppressTracing } from '@opentelemetry/core';
import { CosmosClient } from '@azure/cosmos';

const READINESS_TTL_MS = Number(process.env.READINESS_CACHE_MS ?? 2000);
const BOOT_GRACE_MS = Number(process.env.READINESS_BOOT_GRACE_MS ?? 8000);
const startedAt = Date.now();

const SKIP_DAPR = process.env.SKIP_DAPR === 'true';

function fmt(ms: number) {
    const s = (ms / 1000).toFixed(7);
    const [sec, frac = ''] = s.split('.');
    return `00:00:${sec.padStart(2, '0')}.${frac.padEnd(7, '0')}`;
}

type Entry = {
    data: Record<string, unknown>;
    description: string;
    duration: string;
    status: 'Healthy' | 'Unhealthy';
    tags: string[];
};

type HealthBody = {
    status: 'Healthy' | 'Unhealthy';
    totalDuration: string;
    entries: Record<string, Entry>;
};

export default async function healthRoutes(app: FastifyInstance) {
    app.addHook('onRoute', (r) => {
        r.schema ??= { hide: true };
        r.schema.tags ??= ['health'];
    });

    let lastReadyAt = 0;
    let lastReadyBody: HealthBody | null = null;
    let lastReadyCode = 503;

    async function runReadiness(): Promise<{ code: number; body: HealthBody }> {
        const entries: Record<string, Entry> = {};
        const startAll = performance.now();

        // Self check
        entries['self'] = {
            data: {},
            description: 'HTTP server responding.',
            duration: fmt(0.01),
            status: 'Healthy',
            tags: [],
        };

        if (SKIP_DAPR) {
            const body: HealthBody = {
                status: 'Healthy',
                totalDuration: fmt(performance.now() - startAll),
                entries,
            };
            return { code: 200, body };
        }

        const DAPR = `http://127.0.0.1:${process.env.DAPR_HTTP_PORT ?? 3500}/v1.0`;
        const PUBSUB = process.env.PUBSUB_NAME ?? 'rabbitmq.pubsub';
        const TOPIC = process.env.PUBSUB_HEALTH_TOPIC ?? 'healthCheckTopic';
        const SECRET = process.env.SECRETSTORE_NAME ?? 'local-secret-store';
        const STATE = process.env.STATESTORE_NAME ?? 'statestore.redis';

        async function check(name: string, fn: () => Promise<string>) {
            const start = performance.now();
            try {
                const desc = await fn();
                entries[name] = {
                    data: {},
                    description: desc,
                    duration: fmt(performance.now() - start),
                    status: 'Healthy',
                    tags: [],
                };
            } catch (e: any) {
                const msg = e?.message ?? String(e);
                entries[name] = {
                    data: {},
                    description: msg,
                    duration: fmt(performance.now() - start),
                    status: 'Unhealthy',
                    tags: [],
                };
            }
        }

        // Dapr sidecar
        await check('dapr', async () => {
            await context.with(suppressTracing(context.active()), async () => {
                const r = await fetch(`${DAPR}/healthz`);
                if (r.status !== 204) {
                    throw new Error(`Dapr /healthz returned ${r.status}`);
                }
            });
            return 'Dapr sidecar is healthy.';
        });

        // State store (Redis)
        await check('state store', async () => {
            await context.with(suppressTracing(context.active()), async () => {
                const key = `health:${Date.now()}`;
                let r = await fetch(`${DAPR}/state/${encodeURIComponent(STATE)}`, {
                    method: 'POST',
                    headers: { 'content-type': 'application/json' },
                    body: JSON.stringify([{ key, value: { ok: true, ts: Date.now() } }]),
                });
                if (!r.ok) throw new Error(`State upsert ${r.status}`);

                r = await fetch(`${DAPR}/state/${encodeURIComponent(STATE)}/${key}`);
                if (!r.ok) throw new Error(`State get ${r.status}`);

                r = await fetch(`${DAPR}/state/${encodeURIComponent(STATE)}/${key}`, {
                    method: 'DELETE',
                });
                if (!(r.ok || r.status === 204)) {
                    throw new Error(`State delete ${r.status}`);
                }
            });
            return `Dapr state store: ${STATE} is healthy.`;
        });

        // Secret store
        await check('secret store', async () => {
            await context.with(suppressTracing(context.active()), async () => {
                const r = await fetch(
                    `${DAPR}/secrets/${encodeURIComponent(SECRET)}/bulk`
                );
                if (!r.ok) throw new Error(`Bulk secrets returned ${r.status}`);
            });
            return `Dapr secret store: ${SECRET} is healthy.`;
        });

        // PubSub
        await check('pub sub', async () => {
            await context.with(suppressTracing(context.active()), async () => {
                const r = await fetch(
                    `${DAPR}/publish/${encodeURIComponent(PUBSUB)}/${encodeURIComponent(TOPIC)}`,
                    {
                        method: 'POST',
                        headers: { 'content-type': 'application/json' },
                        body: JSON.stringify({ ping: Date.now() }),
                    }
                );
                if (r.status !== 204) throw new Error(`Publish returned ${r.status}`);
            });
            return `Dapr pubsub: ${PUBSUB} for topic '${TOPIC}' is healthy.`;
        });

        //
        // Cosmos DB
        //
        await check("cosmosdb", async () => {
            const endpoint = process.env.COSMOS_ENDPOINT;
            const key = process.env.COSMOS_KEY;

            const dbId = process.env.COSMOS_DB ?? process.env.COSMOS_DATABASE;
            const containerId = process.env.COSMOS_JOBS_CONTAINER ?? process.env.COSMOS_CONTAINER;

            if (!endpoint || !key || !dbId || !containerId) {
                throw new Error(
                    "Missing CosmosDB configuration. Expected COSMOS_ENDPOINT, COSMOS_KEY, COSMOS_DB, COSMOS_JOBS_CONTAINER."
                );
            }

            await context.with(suppressTracing(context.active()), async () => {
                // 👇 Required to bypass emulator's self-signed cert
                process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";

                const client = new CosmosClient({
                    endpoint,
                    key,
                    connectionPolicy: {
                        enableEndpointDiscovery: false // emulator-safe
                    }
                });

                const { resource: db } = await client.database(dbId).read();
                if (!db) throw new Error(`Database '${dbId}' metadata unreadable.`);

                const { resource: container } =
                    await client.database(dbId).container(containerId).read();

                if (!container)
                    throw new Error(`Container '${containerId}' metadata unreadable.`);
            });

            return `CosmosDB database '${dbId}', container '${containerId}' is healthy.`;
        });

        // Determine health summary
        const unhealthy = Object.values(entries).some(
            (e) => e.status !== 'Healthy'
        );

        const body: HealthBody = {
            status: unhealthy ? 'Unhealthy' : 'Healthy',
            totalDuration: fmt(performance.now() - startAll),
            entries,
        };

        return {
            code: unhealthy ? 503 : 200,
            body,
        };
    }

    async function readinessHandler(_req: any, reply: any) {
        const now = Date.now();

        if (now - startedAt < BOOT_GRACE_MS) {
            const body: HealthBody = {
                status: 'Healthy',
                totalDuration: fmt(0),
                entries: {
                    startup: {
                        data: {},
                        description: `Startup grace window (${BOOT_GRACE_MS} ms)`,
                        duration: fmt(0),
                        status: 'Healthy',
                        tags: ['startup', 'grace'],
                    },
                },
            };
            return reply.code(200).send(body);
        }

        if (lastReadyBody && now - lastReadyAt < READINESS_TTL_MS) {
            return reply.code(lastReadyCode).send(lastReadyBody);
        }

        const { code, body } = await runReadiness();
        lastReadyAt = now;
        lastReadyBody = body;
        lastReadyCode = code;

        return reply.code(code).send(body);
    }

    app.get(
        '/healthzEndpoint',
        {
            schema: {
                hide: true,
                summary: 'Check if components of the system are up or down',
                description:
                    'Health check to see if the components of the system are up or down. Used by the Health Check UI.',
                tags: ['health'],
            },
        },
        readinessHandler
    );

    app.get(
        '/livez',
        {
            schema: {
                hide: true,
                summary: 'Check if the system is up or down',
                description: 'Simple liveness check to see if the system is up or down',
                tags: ['health'],
            },
        },
        async (_req, reply) => reply.code(200).send({ status: 'live' })
    );

    app.get('/readyz', readinessHandler);
}
