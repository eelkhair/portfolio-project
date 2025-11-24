import fp from "fastify-plugin";
import { CosmosService } from "../../../services/cosmos.service.js";
import {
    buildJobText,
    extractJobPayload,
    JobPublishedCloudEvent
} from "../../../schemas/job-published-event.js";
import { OpenAIService } from "../../../services/openai.service.js";
import { z } from "zod";

export default fp<{ cosmosService: CosmosService; openAIService: OpenAIService }>(
    async function jobPublishedListener(app, opts) {

        app.post(
            "/events/job-published",
            {
                schema: {
                    summary: "Dapr pubsub: job.published",
                    tags: ["events"],

                    // ★ ZOD body schema
                    body: JobPublishedCloudEvent,

                    response: {
                        // ★ valid zod schema for empty object
                        200: z.object({}).describe("OK"),

                        // ★ valid zod schema for error response
                        500: z.object({
                            error: z.string()
                        }).describe("Error")
                    }
                }
            },

            async (req, reply) => {
                try {
                    const { cosmosService, openAIService } = opts;

                    // validate body
                    const ce = JobPublishedCloudEvent.parse(req.body);

                    const job = extractJobPayload(ce);
                    const text = buildJobText(job);

                    const embeddings = await openAIService.embed(text);
                    job.model = embeddings.model;
                    job.vector = embeddings.vector;

                    await cosmosService.upsertJob(job);

                    return reply.code(200).send({});
                } catch (err) {
                    app.log.error(
                        { err, body: req.body },
                        "failed to handle job.published"
                    );

                    return reply
                        .code(500)
                        .send({ error: "failed to process" });
                }
            }
        );
    }
);
