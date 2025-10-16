import {z} from "zod";
import fp from "fastify-plugin";
import {CosmosService} from "../../services/cosmos.service.js";
import {FastifyInstance} from "fastify";
import {JobDraft} from "../../schemas/job-draft.js";
import {tracer} from "../../tracing.js";
import {SpanStatusCode} from "@opentelemetry/api";

const Params = z.object({ companyId: z.string().min(1) });
const ErrRes = z.object({error: z.string()});

export default fp<{ cosmosService: CosmosService }>(async (app: FastifyInstance, opts) => {
    const {cosmosService} = opts;
    app.route({
        method: "PUT",
        url: "/drafts/:companyId/upsert",

        schema:{
            tags: ["drafts"],
            params: Params,
            summary: "Create or update draft",
            body: JobDraft,
            response: {
                200: JobDraft,
                400: ErrRes,
                401: ErrRes,
                429: ErrRes,
                500: ErrRes,
            } as const
        },
        handler: async (req, reply) =>
        {

            return tracer.startActiveSpan("handler.ai.drafts.upsert", async (span)=>{
                try {
                    const { companyId } = Params.parse(req.params);
                    const request = JobDraft.parse(req.body);
                    span.addEvent("SaveDraft:start", { companyId: companyId });
                    request.id =await cosmosService.saveDraft(companyId, request);
                    span.addEvent("SaveDraft:ok", {draftId: request.id});
                    reply.status(200);
                    return request;
                }catch (err: any) {
                    const allowed = [400, 401, 429, 500] as const;
                    const status = allowed.includes(err?.status) ? err.status : 500;

                    span.recordException(err);
                    span.setStatus({ code: SpanStatusCode.ERROR, message: "COSMOS_REQUEST_FAILED" });
                    reply.status(status);
                    return { error: "COSMOS_REQUEST_FAILED" };
                } finally {
                    span.end();
                }
            })
        }

    })
})
