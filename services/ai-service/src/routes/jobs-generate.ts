import type { FastifyInstance } from "fastify";
import { z } from "zod";
import OpenAI from "openai";
import {JobGenRequest, JobGenResponse} from "../schemas/job-generate.js";
import {SYSTEM_PROMPT, userPrompt} from "../prompts/job-generate.js";


const Params = z.object({ companyId: z.string().min(1) });

export default async function jobsGenerate(fastify: FastifyInstance) {
  const openai = new OpenAI({ apiKey: process.env.OPENAI_API_KEY! });

  fastify.post("/v1/jobs/:companyId/generate", async (req, reply) => {
    const correlationId = (req.headers["x-correlation-id"] as string) || crypto.randomUUID();
    const idempotencyKey = (req.headers["idempotency-key"] as string) || crypto.randomUUID();

    try {
      const params = Params.parse(req.params);
      const body = JobGenRequest.parse(req.body);

      const messages = [
        { role: "system" as const, content: SYSTEM_PROMPT },
        { role: "user" as const, content: userPrompt({ ...body }) }
      ];

      const completion = await openai.chat.completions.create({
        model: process.env.JOB_GEN_MODEL || "gpt-4o-mini",
        temperature: 0.4,
        response_format: {
          type: "json_schema",
          json_schema: {
            name: "JobGen",
            // Handwritten JSON Schema equivalent of JobGenResponse for portability:
            schema: {
              type: "object",
              additionalProperties: false,
              properties: {
                title: { type: "string", minLength: 6, maxLength: 80 },
                aboutRole: { type: "string", minLength: 60, maxLength: 1500 },
                responsibilities: {
                  type: "array",
                  minItems: 3, maxItems: 8,
                  items: { type: "string", minLength: 3 }
                },
                qualifications: {
                  type: "array",
                  minItems: 3, maxItems: 8,
                  items: { type: "string", minLength: 3 }
                },
                location:{type:"string", maxLength:600},
                notes: { type: "string", minLength: 0, maxLength: 600 },
                metadata: {
                  type: "object",
                  additionalProperties: false,
                  properties: {
                    roleLevel: { enum: ["junior","mid","senior"] },
                    tone: { enum: ["neutral","concise","friendly"] }
                  },
                  required: ["roleLevel", "tone"]
                }
              },
              required: ["title","aboutRole","responsibilities","qualifications","metadata", "notes", "location"],
              $schema: "http://json-schema.org/draft-07/schema#"
            },
            strict: true
          }
        },
        messages
      });

      const raw = completion.choices[0]?.message?.content ?? "{}";
      const parsed = JobGenResponse.parse(JSON.parse(raw));

      // Optional: persist draft in Cosmos (pseudo)
      // await cosmos.upsert({ pk: params.companyId, type: "job_draft", value: parsed })

      return reply
        .code(200)
        .header("x-correlation-id", correlationId)
        .send(parsed);

    } catch (err: any) {
      fastify.log.error({ err }, "job.generate failed");
      const message = err?.message || "Generation failed";
      const status = err?.statusCode || 502;
      return reply.code(status).send({ error: "generation_error", message });
    }
  });
}
