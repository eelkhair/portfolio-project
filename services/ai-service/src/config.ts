import { z } from "zod";

export const Env = z.object({
    OPENAI_API_KEY: z.string(),
    OPENAI_MODEL: z.string(),
    OPENAI_EMBEDDINGS_MODEL: z.string(),

    COSMOS_ENDPOINT: z.string(),
    COSMOS_KEY: z.string(),
    COSMOS_DB: z.string(),
    COSMOS_JOBS_CONTAINER: z.string(),

    PUBSUB_NAME: z.string(),
    STATESTORE_NAME: z.string(),
    SECRETSTORE_NAME: z.string(),
});

export function loadConfig() {
    return Env.parse(process.env);
}
