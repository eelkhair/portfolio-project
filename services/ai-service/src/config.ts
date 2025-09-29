import {z} from "zod";

const Env = z.object({
    OPENAI_API_KEY: z.string().min(1),
    OPENAI_MODEL: z.string().min(1),
    PORT: z.coerce.number().default(6082),
});
export const env = Env.parse(process.env);
