import type { FastifyInstance } from "fastify";
import fp from "fastify-plugin";
import { z } from "zod";
import type { OpenAIService } from "../../services/openai.service.js";
import { JobGenRequest, JobGenResponse } from "../../schemas/job-generate.js";
import { CosmosService } from "../../services/cosmos.service.js";
import { SpanStatusCode } from "@opentelemetry/api";
import {tracer} from "../../tracing.js";

type AiPluginOpts = { openAIService: OpenAIService; cosmosService: CosmosService };
type JobGenResponseT = z.infer<typeof JobGenResponse> & {draftId?: string};

const Params = z.object({ companyId: z.string().min(1) });
const ErrRes = z.object({ error: z.string() });

export default fp<AiPluginOpts>(async (app: FastifyInstance, opts) => {
  const { openAIService, cosmosService } = opts;

  app.route({
    method: "POST",
    url: "/drafts/:companyId/generate",
    schema: {
      tags: ["drafts"],
      summary: "Generate a structured job post draft with OpenAI",
      params: Params,
      body: JobGenRequest,
      response: {
        200: JobGenResponse,
        400: ErrRes,
        401: ErrRes,
        429: ErrRes,
        500: ErrRes,
      } as const,
    },
    handler: async (req, reply) => {
      return tracer.startActiveSpan("handler.ai.drafts.generate", async (span) => {
        try {
          const { companyId } = Params.parse(req.params);
          const body = JobGenRequest.parse(req.body);

          span.addEvent("GenerateWithOpenAI:start", { companyId });
          const result: JobGenResponseT = await openAIService.generateJob(companyId, body);
          span.addEvent("GenerateWithOpenAI:ok");

          span.addEvent("SaveDraft:start", { companyId });
          result.draftId =await cosmosService.saveDraft(companyId, result);
          span.addEvent("SaveDraft:ok", {draftId: result.draftId});
          span.setStatus({ code: SpanStatusCode.OK, message: "Draft generated and saved" });
          return result;
        } catch (err: any) {
          const allowed = [400, 401, 429, 500] as const;
          const status = allowed.includes(err?.status) ? err.status : 500;

          span.recordException(err);
          span.setStatus({ code: SpanStatusCode.ERROR, message: "AI_REQUEST_FAILED" });
          reply.status(status);
          return { error: "AI_REQUEST_FAILED" };
        } finally {
          span.end();
        }
      });
    },
  });
});
