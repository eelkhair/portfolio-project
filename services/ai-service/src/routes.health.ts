// src/routes.health.ts (ESM/TS)
import type {FastifyInstance} from 'fastify';
import {performance} from 'node:perf_hooks';
import {context} from "@opentelemetry/api";
import {suppressTracing} from '@opentelemetry/core'

const READINESS_TTL_MS = Number(process.env.READINESS_CACHE_MS ?? 2000);

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
    // add a "health" tag to these routes for your Swagger/OpenAPI UI
    app.addHook('onRoute', (r) => {
        r.schema ??= {};
        r.schema.tags ??= ['health'];
    });

    let lastReadyAt = 0;
    let lastReadyBody: HealthBody | null = null;
    let lastReadyCode = 503;

    async function runReadiness(): Promise<{ code: number; body: HealthBody }> {
        const DAPR = `http://127.0.0.1:${process.env.DAPR_HTTP_PORT ?? 6083}/v1.0`;
        const PUBSUB = process.env.PUBSUB_NAME ?? 'rabbitmq.pubsub';
        const TOPIC = process.env.PUBSUB_HEALTH_TOPIC ?? 'healthCheckTopic';
        const SECRET = process.env.SECRETSTORE_NAME ?? 'local-secret-store';
        const STATE = process.env.STATESTORE_NAME ?? 'statestore.redis';

        const entries: Record<string, Entry> = {};
        const startAll = performance.now();

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
                if (r.status !== 204) throw new Error(`Dapr /healthz returned ${r.status}`);
            });
            return 'Dapr sidecar is healthy.';
        });

        // State store
        await check('state store', async () => {
            await context.with(suppressTracing(context.active()), async () => {
                const key = `health:${Date.now()}`;
                let r = await fetch(`${DAPR}/state/${encodeURIComponent(STATE)}`, {
                    method: 'POST',
                    headers: {'content-type': 'application/json'},
                    body: JSON.stringify([{key, value: {ok: true, ts: Date.now()}}]),
                });
                if (!r.ok) throw new Error(`State upsert ${r.status}`);
                r = await fetch(`${DAPR}/state/${encodeURIComponent(STATE)}/${key}`);
                if (!r.ok) throw new Error(`State get ${r.status}`);
                r = await fetch(`${DAPR}/state/${encodeURIComponent(STATE)}/${key}`, {
                    method: 'DELETE',
                });
                if (!(r.ok || r.status === 204)) throw new Error(`State delete ${r.status}`);
            });
            return `Dapr state store: ${STATE} is healthy.`;

        });

        // Secret store
        await check('secret store', async () => {
            await context.with(suppressTracing(context.active()), async () => {
                const r = await fetch(`${DAPR}/secrets/${encodeURIComponent(SECRET)}/bulk`);
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
                        headers: {'content-type': 'application/json'},
                        body: JSON.stringify({ping: Date.now()}),
                    }
                );
                if (r.status !== 204) throw new Error(`Publish returned ${r.status}`);
            });
            return `Dapr pubsub: ${PUBSUB} for topic '${TOPIC}' is healthy.`;
        });

        // Self row
        entries['self'] = {
            data: {},
            description: 'HTTP server responding.',
            duration: fmt(0.01),
            status: 'Healthy',
            tags: [],
        };

        const unhealthy = Object.values(entries).some((e) => e.status !== 'Healthy');
        const body: HealthBody = {
            status: unhealthy ? 'Unhealthy' : 'Healthy',
            totalDuration: fmt(performance.now() - startAll),
            entries,
        };
        const code = unhealthy ? 503 : 200;
        return {code, body};
    }

    async function readinessHandler(_req: any, reply: any) {
        const now = Date.now();
        if (lastReadyBody && now - lastReadyAt < READINESS_TTL_MS) {
            return reply.code(lastReadyCode).send(lastReadyBody);
        }
        const {code, body} = await runReadiness();
        lastReadyAt = now;
        lastReadyBody = body;
        lastReadyCode = code;
        return reply.code(code).send(body);
    }

    app.get(
        '/healthzEndpoint',
        {
            schema: {
                summary: 'check if components of the system are up or down',
                description:
                    'health check to see if the components of the system are up or down. Used by the Health Check app',
            },
        },
        readinessHandler
    );

    app.get(
        '/livez',
        {
            schema: {
                summary: 'check if the system is up or down',
                description: 'health check to see the if the system is up or down',
            },
        },
        async (_req, reply) => reply.code(200).send({status: 'live'})
    );

    // Hidden from Swagger (and we ignore it from tracing too)
    app.get('/readyz', {schema: {hide: true}}, readinessHandler);
}
