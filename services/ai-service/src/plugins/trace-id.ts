import { FastifyPluginAsync } from "fastify";
import { context, trace } from "@opentelemetry/api";

function getTraceId(req: any): string | undefined {
    const span = trace.getSpan(context.active());
    const fromSpan = span?.spanContext().traceId;
    if (fromSpan) return fromSpan;

    const tp: string | undefined = req.headers?.["traceparent"];
    if (!tp) return undefined;
    const parts = String(tp).split("-");
    return parts.length >= 3 ? parts[1] : undefined;
}

const plugin: FastifyPluginAsync = async (app) => {
    // Attach traceId early and enrich logs
    app.addHook("onRequest", async (req) => {
        const traceId = getTraceId(req);
        if (traceId) {
            (req as any).traceId = traceId;
            req.log = req.log.child({ traceId });
        }
    });

    app.addHook("onSend", async (req, reply, payload) => {
        const traceId = (req as any).traceId ?? getTraceId(req);
        if (traceId) reply.header("x-trace-id", traceId);
        return payload;
    });
};

export default plugin;
