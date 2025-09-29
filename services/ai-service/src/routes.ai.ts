import type { FastifyInstance } from "fastify";
import fp from "fastify-plugin";
import { z } from "zod";
import { OpenAIService } from "./lib/openai.service.js";

type AiPluginOpts = { service: OpenAIService };

export default fp<AiPluginOpts>(async (app: FastifyInstance, opts) => {
    const { service } = opts;

    const Query  = z.object({ q: z.string().min(1) });
    const Reply  = z.object({ text: z.string() });
    const ErrRes = z.object({ error: z.string() });

    app.route({
        method: "GET",
        url: "/ai/rewrite",
        schema: {
            tags: ["ai"],
            summary: "Rewrite a short prompt with OpenAI",
            querystring: Query,
            response: {
                200: Reply,
                400: ErrRes,
                401: ErrRes,
                429: ErrRes,
                500: ErrRes,
            } as const,
        },
        handler: async (req, reply) => {
            try {
                const { q } = Query.parse(req.query);
                const result = await service.rewrite(q);
                return reply.send(result);
            } catch (err: any) {
                const allowed = [400, 401, 429, 500] as const;
                const status = allowed.includes(err?.status) ? err.status : 500;
                app.log.error({ err }, "OpenAI request failed");
                return reply.status(status).send({ error: "AI_REQUEST_FAILED" });
            }
        },
    });
});
