import {z} from "zod";
import fp from "fastify-plugin";
import {FastifyInstance} from "fastify";
import {CosmosService} from "../../services/cosmos.service.js";
import {JobDraft} from "../../schemas/job-draft.js";
import {tracer} from "../../tracing.js";
import {SpanStatusCode} from "@opentelemetry/api";

const Params = z.object({ companyId: z.string().min(1) });
const ErrRes = z.object({error: z.string()});

export default fp<{cosmosService:CosmosService}>(async (app: FastifyInstance, opts) => {
    const {cosmosService} = opts;
    app.route({
        method: "GET",
        url:"/drafts/:companyId",
        schema:{
            tags:["drafts"],
            params: Params,
            summary: "Retrieve list of drafts for company",
            response:{
                200: z.array(JobDraft),
                400: ErrRes,
                401: ErrRes,
                429: ErrRes,
                500: ErrRes
            } as const
        },
        handler: async (req, reply) => {
            return tracer.startActiveSpan("handler.ai.drafts.list", async (span)=>{
                try{
                    const {companyId} = Params.parse(req.params);
                    span.addEvent("ListDrafts:start", {companyId: companyId});
                    const response = await cosmosService.listDrafts(companyId);
                    span.addEvent("ListDrafts:ok", response.length);
                    reply.status(200)
                    return response;
                }catch(err:any){
                    const allowed = [400, 401, 429, 500] as const;
                    const status = allowed.includes(err?.status) ? err.status : 500;
                    span.recordException(err);
                    span.setStatus({ code: SpanStatusCode.ERROR, message: "AI_REQUEST_FAILED" });
                    reply.status(status);
                    return { error: "COSMOS_REQUEST_FAILED" };
                }

            })
        }
    })
})
