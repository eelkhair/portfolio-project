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

import devRoutes from "./routes/routes.dev.js";
import daprRoutes from "./routes/routes.dapr.js";
import healthRoutes from "./routes/routes.health.js";
import traceIdPlugin from "./plugins/trace-id.js";
import aiRoutes from "./routes/routes.ai.js";
import {OpenAIService} from "./lib/openai.service.js";
import {env} from "./config.js";
import jobsGenerate from "./routes/jobs-generate.js"; // ← extension-less

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


const service = new OpenAIService({ apiKey: env.OPENAI_API_KEY, model: env.OPENAI_MODEL });

await app.register(healthRoutes); // livez/readyz/healthzEndpoint
await app.register(devRoutes);
await app.register(daprRoutes);
await app.register(aiRoutes,{service});
await app.register(jobsGenerate);

const port = Number(process.env.PORT ?? 6082);
await app.listen({ port, host: "0.0.0.0" });
