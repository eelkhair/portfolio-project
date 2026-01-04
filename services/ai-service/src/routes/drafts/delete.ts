import { z } from "zod";
import fp from "fastify-plugin";
import { CosmosService } from "../../services/cosmos.service.js";
import { FastifyInstance } from "fastify";
import { tracer } from "../../tracing.js";
import { SpanStatusCode } from "@opentelemetry/api";

const Params = z.object({
    id: z.string().min(1),
    companyId: z.string().min(1)
});

const ErrRes = z.object({
    error: z.string()
});

export default fp<{ cosmosService: CosmosService }>(
    async (app: FastifyInstance, opts) => {
        const { cosmosService } = opts;

        app.route({
            method: "DELETE",
            url: "/drafts/:companyId/:id",

            schema: {
                tags: ["drafts"],
                summary: "Delete draft",
                params: Params,

                response: {
                    204: z.null().describe("No Content"),

                    400: ErrRes,
                    401: ErrRes,
                    429: ErrRes,
                    500: ErrRes
                }
            },

            handler: async (req, reply) => {
                return tracer.startActiveSpan(
                    "handler.ai.drafts.delete",
                    async (span) => {
                        try {
                            const { id, companyId } = Params.parse(req.params);

                            span.addEvent("DeleteDraft:start", { draftId: id });

                            await cosmosService.deleteDraft(id, companyId);

                            span.addEvent("DeleteDraft:ok", { draftId: id });

                            reply.status(204).send();
                        } catch (err: any) {
                            const allowed = [400, 401, 429, 500] as const;
                            const status =
                                allowed.includes(err?.status) ? err.status : 500;

                            span.recordException(err);
                            span.setStatus({
                                code: SpanStatusCode.ERROR,
                                message: "COSMOS_REQUEST_FAILED"
                            });

                            reply.status(status);
                            return { error: "COSMOS_REQUEST_FAILED" };
                        } finally {
                            span.end();
                        }
                    }
                );
            }
        });
    }
);
