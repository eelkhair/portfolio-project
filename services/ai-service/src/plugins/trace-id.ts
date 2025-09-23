import { FastifyPluginAsync } from "fastify";
import { context, trace } from "@opentelemetry/api";

/** Safely get the current traceId; fallback to W3C traceparent header. */
function getTraceId(req: any): string | undefined {
    const span = trace.getSpan(context.active());
    const fromSpan = span?.spanContext().traceId;
    if (fromSpan) return fromSpan;

    const tp: string | undefined = req.headers?.["traceparent"];
    if (!tp) return undefined;
    // "00-<traceId>-<spanId>-flags"
    const parts = String(tp).split("-");
    return parts.length >= 3 ? parts[1] : undefined;
}

const plugin: FastifyPluginAsync = async (app) => {
    // Attach traceId early and enrich logs
    app.addHook("onRequest", async (req) => {
        const traceId = getTraceId(req);
        if (traceId) {
            (req as any).traceId = traceId;
            // Enrich the per-request logger
            req.log = req.log.child({ traceId });
        }
    });

    // Always echo x-trace-id on responses
    app.addHook("onSend", async (req, reply, payload) => {
        const traceId = (req as any).traceId ?? getTraceId(req);
        if (traceId) reply.header("x-trace-id", traceId);
        return payload;
    });
};

export default plugin;
