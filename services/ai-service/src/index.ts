import "dotenv/config";
import Fastify from "fastify";
import cors from "@fastify/cors";
import swagger from "@fastify/swagger";
import swaggerUi from "@fastify/swagger-ui";
import {
    ZodTypeProvider,
    jsonSchemaTransform,
    validatorCompiler,
    serializerCompiler
} from "fastify-type-provider-zod";
//import devRoutes from "./routes/jobs/upsert.js";
import daprRoutes from "./routes/utils/dapr.js";
import healthRoutes from "./routes/utils/health.js";
import traceIdPlugin from "./plugins/trace-id.js";
import aiRoutes from "./routes/drafts/rewrite.js";
import draftUpsert from "./routes/drafts/upsert.js";
import draftDelete from "./routes/drafts/delete.js";
import draftList from "./routes/drafts/list.js";

import {env} from "./config.js";
import jobsGenerate from "./routes/drafts/generate.js";
import {CosmosService} from "./services/cosmos.service.js";
import {OpenAIService} from "./services/openai.service.js";
import {startOtel, stopOtel} from "./otel.js";
import {tracer} from "./tracing.js";


await startOtel();
const app = Fastify({ logger: true }).withTypeProvider<ZodTypeProvider>();
app.setValidatorCompiler(validatorCompiler);
app.setSerializerCompiler(serializerCompiler);
await app.register(traceIdPlugin);

await app.register(cors, { origin: true });
await app.register(swagger, {
    openapi: { info: { title: "AI Service", version: "1.0.0" } },
    transform: jsonSchemaTransform,
});
await app.register(swaggerUi, { routePrefix: "/docs",
    uiConfig: {
        docExpansion: 'none',           // collapse groups
        tagsSorter: 'alpha',
        operationsSorter: 'alpha',
        defaultModelsExpandDepth: -1,   // hide "Schemas/Models"
    },
});

    app.get("/api/env", {schema:{hide: true}}, async () => {
        return {
            NODE_ENV: process.env.NODE_ENV,
            PORT: process.env.PORT,
            PUBSUB_NAME: process.env.PUBSUB_NAME,
            STATESTORE_NAME: process.env.STATESTORE_NAME,
            SECRETSTORE_NAME: process.env.SECRETSTORE_NAME,
            exporter: process.env.OTEL_EXPORTER_OTLP_TRACES_ENDPOINT,
            zipkin: process.env.OTEL_EXPORTER_ZIPKIN_ENDPOINT
        };
    });

app.get("/", {schema:{hide: true}}, async (request, reply): Promise<any> => reply.redirect("/docs") )


const openAIService = new OpenAIService({ apiKey: env.OPENAI_API_KEY, model: env.OPENAI_MODEL });
const cosmosService = new CosmosService();
await app.register(healthRoutes);
await app.register(daprRoutes);
await app.register(draftUpsert,{cosmosService});
await app.register(draftDelete,{cosmosService});
await app.register(draftList, {cosmosService});
await app.register(aiRoutes,{service: openAIService});
await app.register(jobsGenerate, {openAIService: openAIService, cosmosService: cosmosService});
app.get('/ping', async (_req, _rep) => {
    return await tracer.startActiveSpan('handler.ping', async (span) => {
        try {
            return { pong: true };
        } finally {
            span.end();
        }
    });
});
const port = Number(process.env.PORT ?? 6082);

process.on('SIGINT', async () => { await app.close(); await stopOtel(); process.exit(0); });
process.on('SIGTERM', async () => { await app.close(); await stopOtel(); process.exit(0); });

await app.listen({ port, host: "0.0.0.0" });
