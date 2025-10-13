import type { FastifyInstance } from "fastify";
import fp from "fastify-plugin";
import { z } from "zod";
import type { OpenAIService } from "../../services/openai.service.js";
import {JobGenRequest, JobGenResponse} from "../../schemas/job-generate.js";
import {CosmosService} from "../../services/cosmos.service.js";

type AiPluginOpts = { openAIService: OpenAIService, cosmosService: CosmosService };
const Params = z.object({ companyId: z.string().min(1) });
type JobGenResponseT = z.infer<typeof JobGenResponse>;

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
        500: ErrRes
      } as const
    },
    handler: async (req, reply) => {
      try {
        const params = Params.parse(req.params);
        const body = JobGenRequest.parse(req.body);
        const result: JobGenResponseT = await openAIService.generateJob(params.companyId, body);
        await cosmosService.saveDraft(params.companyId, result);

        return reply.send(result);
      } catch (err: any) {
        const allowed = [400, 401, 429, 500] as const;
        const status = allowed.includes(err?.status) ? err.status : 500;
        app.log.error({ err }, "OpenAI job.generate failed");
        return reply.status(status).send({ error: "AI_REQUEST_FAILED" });
      }
    }
  });
});
