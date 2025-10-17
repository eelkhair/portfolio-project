import { z } from "zod";

export const JobRewriteItemRequest = z.object({
    // What are we rewriting?
    field: z.enum(["title", "aboutRole", "responsibilities", "qualifications"]),
    // The single snippet to rewrite (bullet line, title text, or aboutRole fragment)
    value: z.string().min(3).max(5_000),

    // Optional context to keep the rewrite on-brand/accurate
    context: z.object({
        title: z.string().min(3).max(160).optional(),
        aboutRole: z.string().min(10).max(10_000).optional(),
        responsibilities: z.array(z.string().min(3)).max(30).optional(),
        qualifications: z.array(z.string().min(3)).max(30).optional(),
    }).optional(),

    // Optional style controls
    style: z.object({
        tone: z.enum(["neutral", "professional", "friendly", "concise", "enthusiastic"]).optional(),
        formality: z.enum(["casual", "neutral", "formal"]).optional(),
        audience: z.string().max(120).optional(),
        maxWords: z.number().int().min(10).max(400).optional(),
        language: z.string().min(2).max(5).optional(), // e.g. "en"
        avoidPhrases: z.array(z.string().min(2)).max(20).optional(),
        bulletsPerSection: z.number().int().min(3).max(12).optional(), // in case you later allow multi-bullet ops
        includeEEOBoilerplate: z.boolean().optional(),
    }).optional(),
});

export type JobRewriteItemRequest = z.infer<typeof JobRewriteItemRequest>;

export const JobRewriteItemResponse = z.object({
    field: z.enum(["title", "aboutRole", "responsibilities", "qualifications"]),
    rewritten: z.string(), // single rewritten string
    meta: z.object({
        model: z.string(),
        promptTokens: z.number().optional(),
        completionTokens: z.number().optional(),
        totalTokens: z.number().optional(),
        finishReason: z.string().optional(),
    }),
});

export type JobRewriteItemResponse = z.infer<typeof JobRewriteItemResponse>;
