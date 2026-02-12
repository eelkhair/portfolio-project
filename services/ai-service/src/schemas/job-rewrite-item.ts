import { z } from "zod";

/** Literal enums */
export const ToneLiterals = [
    "neutral",
    "professional",
    "friendly",
    "concise",
    "enthusiastic",
] as const;

export const FormalityLiterals = ["casual", "neutral", "formal"] as const;

export const FieldLiterals = [
    "title",
    "aboutRole",
    "responsibilities",
    "qualifications",
] as const;

const ToneEnum = z.enum(ToneLiterals);
const FormalityEnum = z.enum(FormalityLiterals);
const FieldEnum = z.enum(FieldLiterals);

/** Helpers */

// Trim an unknown → string, then validate with a string schema
const trimTo = <T extends z.ZodTypeAny>(schema: T) =>
    z.preprocess((v) => (typeof v === "string" ? v.trim() : v), schema);

// Lowercase +trim before enum validation (safe for any ZodEnum)
const toLowerEnum = (e: z.ZodEnum<any>) =>
    z.preprocess(
        (v) => (typeof v === "string" ? v.trim().toLowerCase() : v),
        e
    );

// Array of trimmed strings with per-item min and optional max array length
const trimmedStringArray = (minPerItem: number, maxItems?: number) => {
    let arr = z.array(trimTo(z.string().min(minPerItem)));
    if (typeof maxItems === "number") arr = arr.max(maxItems);
    return arr;
};

/** ------------------------------
 *  REQUEST
 * ------------------------------ */
export const JobRewriteItemRequest = z.object({
    field: FieldEnum,

    // single value to rewrite
    value: trimTo(z.string().min(3).max(5_000)),

    // optional context to guide the rewrite
    context: z
        .object({
            title: trimTo(z.string().min(3).max(160)).optional(),
            aboutRole: trimTo(z.string().min(10).max(10_000)).optional(),
            responsibilities: trimmedStringArray(3, 30).optional(),
            qualifications: trimmedStringArray(3, 30).optional(),
            companyName: trimTo(z.string().max(160)).optional(),
        })
        .optional(),

    // optional style controls
    style: z
        .object({
            tone: toLowerEnum(ToneEnum).optional(),
            formality: toLowerEnum(FormalityEnum).optional(),
            audience: trimTo(z.string().max(120)).optional(),
            maxWords: z.coerce.number().int().min(10).max(2000).optional(),
            numParagraphs: z.coerce.number().int().min(1).max(4).optional(),
            language: z
                .preprocess(
                    (v) => (typeof v === "string" ? v.trim().toLowerCase() : v),
                    z.string().regex(/^[a-z]{2,5}$/, "language must be ISO code like 'en'")
                )
                .optional(),
            avoidPhrases: trimmedStringArray(2, 20).optional(),
            bulletsPerSection: z.coerce.number().int().min(3).max(12).optional(),
            includeEEOBoilerplate: z.coerce.boolean().optional(),
        })
        .optional(),
}).superRefine((val, ctx) => {
    // If field is aboutRole, companyName must be provided
    if (val.field === "aboutRole") {
        const name = val.context?.companyName?.trim();
        if (!name) {
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                message: "context.companyName is required when field is 'aboutRole'",
                path: ["context", "companyName"],
            });
        }
    }
});


export type JobRewriteItemRequest = z.infer<typeof JobRewriteItemRequest>;

/** ------------------------------
 *  RESPONSE
 * ------------------------------ */
export const JobRewriteItemResponse = z.object({
    field: FieldEnum,
    // exactly 3 suggestions back
    options: z.array(trimTo(z.string().min(3))).length(3),
    meta: z.object({
        model: z.string(),
        promptTokens: z.number().optional(),
        completionTokens: z.number().optional(),
        totalTokens: z.number().optional(),
        finishReason: z.string().optional(),
    }),
});

export type JobRewriteItemResponse = z.infer<typeof JobRewriteItemResponse>;
