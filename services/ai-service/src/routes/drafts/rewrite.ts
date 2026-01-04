import type { FastifyInstance } from "fastify";
import fp from "fastify-plugin";
import { z } from "zod";
import { OpenAIService } from "../../services/openai.service.js";
import { JobRewriteItemRequest, JobRewriteItemResponse } from "../../schemas/job-rewrite-item.js";

type AiPluginOpts = { service: OpenAIService };

const ErrRes  = z.object({ error: z.string() });

export default fp<AiPluginOpts>(async (app: FastifyInstance, opts) => {
    const { service } = opts;

    app.route({
        method: "PUT",
        url: "/drafts/rewrite/item",
        schema: {
            tags: ["drafts"],
            summary: "Rewrite a single field item (no persistence)",
            body: JobRewriteItemRequest,
            response: {
                200: JobRewriteItemResponse,
                400: ErrRes,
                401: ErrRes,
                429: ErrRes,
                500: ErrRes,
            } as const,
        },
        handler: async (req, reply) => {
            try {
                const body = JobRewriteItemRequest.parse(req.body);
                console.log(body)
                const result = await service.rewriteItem(body);
                console.log(result)
                return reply.send(result);
            } catch (err: any) {
                const allowed = [400, 401, 429, 500] as const;
                const status = allowed.includes(err?.status) ? err.status : 500;
                app.log.error({ err }, "AI rewrite-item request failed");
                return reply.status(status).send({ error: "AI_REQUEST_FAILED" });
            }
        },
    });
});
