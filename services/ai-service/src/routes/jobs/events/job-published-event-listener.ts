import fp from "fastify-plugin";
import {CosmosService} from "../../../services/cosmos.service.js";
import {buildJobText, extractJobPayload, JobPublishedCloudEvent} from "../../../schemas/job-published-event.js";
import {OpenAIService} from "../../../services/openai.service.js";

export default fp<{ cosmosService: CosmosService, openAIService:OpenAIService }>(async function jobPublishedListener(app,
                                                                                        opts) {
    app.post("/events/job-published", {
        schema: {
            summary: "Dapr pubsub: job.published",
            tags: ["events", "dapr"],
            body: JobPublishedCloudEvent, // Let zod do strict validation
            response: {
                200: { type: "null" },
                500: { type: "object", properties: { error: { type: "string" } } }
            }
        }
    }, async (req, reply) => {
        try {
            const { cosmosService, openAIService } = opts;
            const ce   = JobPublishedCloudEvent.parse(req.body);

            const job = extractJobPayload(ce);
            const text = buildJobText(job)

            const embeddings = await openAIService.embed(text);
            job.model = embeddings.model;
            job.vector = embeddings.vector;


            await cosmosService.upsertJob(job);

            return reply.code(200).send();
        } catch (err) {
            app.log.error({ err, body: req.body }, "failed to handle job.published");
            // Non-2xx triggers Dapr retry according to backoff & component settings
            return reply.code(500).send({ error: "failed to process" });
        }
    });
});
