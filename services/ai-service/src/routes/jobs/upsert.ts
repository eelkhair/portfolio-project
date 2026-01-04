// import type { FastifyInstance } from "fastify";
// import { z } from "zod";
// import {tracer} from "../../tracing.js";
// import {SpanStatusCode} from "@opentelemetry/api";
//
// export default async function devRoutes(app: FastifyInstance) {
//     app.addHook('onRoute', (r) => {
//         r.schema ??= {};
//         r.schema.tags ??= ['jobs'];
//     });
//     const UpsertBody = z.object({
//         id: z.string().min(1),
//         companyid: z.string().min(1),
//         test: z.string().optional()
//     });
//
//     app.post("/jobs/upsert", { schema: { body: UpsertBody } }, async (req) => {
//         const { id, companyid, test } = req.body as z.infer<typeof UpsertBody>;
//         const c = getContainer(process.env.COSMOS_JOBS_CONTAINER!);
//         const { resource } = await c.items.upsert({ id, companyid, test });
//         return { ok: true, id: resource?.id };
//     });
//
//     app.get('/jobs/:companyid/:id', async (req, reply) =>
//         tracer.startActiveSpan('handler.ai.job.read', async (span) => {
//             const { companyid, id } = req.params as { companyid: string; id: string };
//
//             // useful attributes for filtering/searching in Jaeger/Zipkin
//             span.setAttributes({
//                 'http.route': '/ai/job/:companyid/:id',
//                 'job.company_id': companyid,
//                 'job.id': id,
//                 'db.system': 'azure-cosmos',
//                 'db.operation': 'read',
//             });
//
//             try {
//                 span.addEvent('cosmos.read.begin');
//                 const c = getContainer(process.env.COSMOS_JOBS_CONTAINER!);
//                 const res = await c.item(id, companyid).read();
//                 span.addEvent('cosmos.read.ok', { statusCode: 200 });
//                 span.setStatus({ code: SpanStatusCode.OK });
//
//                 // (optional) expose trace id to the client for troubleshooting
//                 reply.header('x-trace-id', span.spanContext().traceId);
//
//                 return res.resource;
//             } catch (err: any) {
//                 span.recordException(err);
//                 span.setStatus({ code: SpanStatusCode.ERROR, message: err?.message });
//                 return reply.code(404).send({ error: 'not found' });
//             }
//         })
//     );
// }
