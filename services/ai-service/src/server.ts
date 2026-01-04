// src/server.ts
import Fastify from "fastify";
import {startOtel, stopOtel} from "./otel.js";
import {jsonSchemaTransform, serializerCompiler, validatorCompiler, ZodTypeProvider} from "fastify-type-provider-zod";
import traceIdPlugin from "./plugins/trace-id.js";
import cors from "@fastify/cors";
import swagger from "@fastify/swagger";
import swaggerUi from "@fastify/swagger-ui";
import {OpenAIService} from "./services/openai.service.js";
import {CosmosService} from "./services/cosmos.service.js";
import healthRoutes from "./routes/utils/health.js";
import daprRoutes from "./routes/utils/dapr.js";
import draftUpsert from "./routes/drafts/upsert.js";
import draftDelete from "./routes/drafts/delete.js";
import draftList from "./routes/drafts/list.js";
import jobPublishedEventListener from "./routes/jobs/events/job-published-event-listener.js";
import aiRoutes from "./routes/drafts/rewrite.js";
import jobsGenerate from "./routes/drafts/generate.js";
import {tracer} from "./tracing.js";

export async function startServer(config: any) {
    await startOtel();
    const app = Fastify({ logger: true }).withTypeProvider<ZodTypeProvider>();
    app.setValidatorCompiler(validatorCompiler);
    app.setSerializerCompiler(serializerCompiler);
    app.register(cors, {
        origin: [
            "http://192.168.1.112:9000",
            "https://swagger.eelkhair.net"
        ],
        methods: ["GET", "POST", "PUT", "DELETE"],
        allowedHeaders: ["Content-Type", "Authorization"]
    });
    await app.register(traceIdPlugin);
    await app.register(swagger, {
        openapi: { info: { title: "AI Service", version: "1.0.0" } },
        transform: jsonSchemaTransform,
    });

    await app.register(swaggerUi, {
        routePrefix: "/docs",
        uiConfig: {
            docExpansion: "none",
            tagsSorter: "alpha",
            operationsSorter: "alpha",
            defaultModelsExpandDepth: -1,
        },
    });
    app.get("/api/env", { schema: { hide: true } }, async () => ({
        NODE_ENV: process.env.NODE_ENV,
        PORT: process.env.PORT,
        PUBSUB_NAME: process.env.PUBSUB_NAME,
        STATESTORE_NAME: process.env.STATESTORE_NAME,
        SECRETSTORE_NAME: process.env.SECRETSTORE_NAME,
        exporter: process.env.OTEL_EXPORTER_OTLP_TRACES_ENDPOINT,
        zipkin: process.env.OTEL_EXPORTER_ZIPKIN_ENDPOINT,
    }));

    app.get("/", { schema: { hide: true } }, async (_req, reply) =>
        reply.redirect("/docs")
    );
    app.addContentTypeParser(
        "application/cloudevents+json",
        { parseAs: "string" },
        (req, body, done) => {
            try {
                done(null, JSON.parse(body as string));
            } catch (err) {
                done(err as Error);
            }
        }
    );
    const openAIService = new OpenAIService({
        apiKey: config.OPENAI_API_KEY,
        model: config.OPENAI_MODEL,
    });

    const cosmosService = new CosmosService();


    await app.register(healthRoutes);
    await app.register(daprRoutes);

    await app.register(draftUpsert, { cosmosService });
    await app.register(draftDelete, { cosmosService });
    await app.register(draftList, { cosmosService });
    await app.register(jobPublishedEventListener, { cosmosService, openAIService });

    await app.register(aiRoutes, { service: openAIService });
    await app.register(jobsGenerate, { openAIService, cosmosService });

    app.get("/ping", { schema: { hide: true } }, async () => {
        return tracer.startActiveSpan("handler.ping", async (span) => {
            try {
                return { pong: true };
            } finally {
                span.end();
            }
        });
    });
    const port = Number(process.env.PORT ?? 6082);

    process.on("SIGINT", async () => {
        await app.close();
        await stopOtel();
        process.exit(0);
    });

    process.on("SIGTERM", async () => {
        await app.close();
        await stopOtel();
        process.exit(0);
    });
    await app.listen({
        port: 6082,
        host: "0.0.0.0",
    });

    return app;
}
