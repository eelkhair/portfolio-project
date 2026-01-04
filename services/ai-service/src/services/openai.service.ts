import {OpenAI} from "openai";
import {JobGenRequest, JobGenResponse} from "../schemas/job-generate.js";
import {SYSTEM_PROMPT, userPrompt} from "../prompts/job-generate.js";
import {JobRewriteItemRequest} from "../schemas/job-rewrite-item.js";

export class OpenAIService{
    private client: OpenAI;
    private readonly model: string;
    constructor(opts: { apiKey: string; model: string }) {
        this.client = new OpenAI({ apiKey: opts.apiKey });
        this.model = opts.model;
    }

    async generateJob(companyId: string, body: unknown) {
        const req = JobGenRequest.parse(body);

        const messages = [
            { role: "system" as const, content: SYSTEM_PROMPT },
            { role: "user" as const, content: userPrompt(req) }
        ];

        const jsonSchema = {
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
                // notes and location are required in strict mode; allow empty string for both
                notes: { type: "string", minLength: 0, maxLength: 600 },
                location: { type: "string", minLength: 0, maxLength: 120 },
                metadata: {
                    type: "object",
                    additionalProperties: false,
                    properties: {
                        roleLevel: { enum: ["junior","mid","senior","staff","principal"] },
                        tone: { enum: ["neutral","concise","friendly"] }
                    },
                    required: ["roleLevel","tone"]
                }
            },
            required: [
                "title","aboutRole","responsibilities",
                "qualifications","notes","location","metadata"
            ],
            $schema: "http://json-schema.org/draft-07/schema#"
        } as const;

        const completion = await this.client.chat.completions.create({
            model: this.model,
            response_format: {
                type: "json_schema",
                json_schema: { name: "JobGen", schema: jsonSchema, strict: true }
            },
            messages
        });

        const raw = completion.choices[0]?.message?.content ?? "{}";
        let parsed: unknown;
        try {
            parsed = JSON.parse(raw);
        } catch (e) {
            console.error("Non-JSON response from model:", raw);
            const error: any = new Error("Model returned non-JSON");
            error.status = 500;
            throw error;
        }

        return JobGenResponse.parse(parsed);
    }
    async rewriteItem(input: JobRewriteItemRequest, { timeoutMs = 60000 } = {}) {
        const ac = new AbortController();
        const t = setTimeout(() => ac.abort(), timeoutMs);
        const mustIncludeCompany = input.field === "aboutRole";
        const companyName = input.context?.companyName?.trim();

        if (mustIncludeCompany && !companyName) {
            const err: any = new Error("context.companyName is required for aboutRole rewrites");
            err.status = 400;
            throw err;
        }
        const schema = {
            type: "object",
            additionalProperties: false,
            properties: {
                options: {
                    type: "array",
                    minItems: 3,
                    maxItems: 3,
                    items: { type: "string", minLength: 3, maxLength: 5000 }
                }
            },
            required: ["options"],
            $schema: "http://json-schema.org/draft-07/schema#",
        } as const;

        const style = input.style ?? {};
        const hints: string[] = [];
        if (style.tone) hints.push(`Tone: ${style.tone}`);
        if (style.formality) hints.push(`Formality: ${style.formality}`);
        if (style.audience) hints.push(`Audience: ${style.audience}`);
        if (style.maxWords) hints.push(`Max words: ${style.maxWords}`);
        if (style.numParagraphs) hints.push(`Number of paragraphs: ${style.numParagraphs}`);
        if (style.language) hints.push(`Language: ${style.language}`);
        if (style.avoidPhrases?.length) hints.push(`Avoid: ${style.avoidPhrases.join(", ")}`);
        if (style.includeEEOBoilerplate) hints.push(`Include brief EEO-friendly phrasing when appropriate`);
        const ctxLines: string[] = [];
        const ctx = input.context ?? {};
        if (ctx.title) ctxLines.push(`title: ${ctx.title}`);
        if (ctx.aboutRole) ctxLines.push(`aboutRole: ${ctx.aboutRole}`);
        if (ctx.responsibilities?.length) ctxLines.push(`responsibilities: ${JSON.stringify(ctx.responsibilities)}`);
        if (ctx.qualifications?.length) ctxLines.push(`qualifications: ${JSON.stringify(ctx.qualifications)}`);
        if (ctx.companyName) ctxLines.push(`companyName: ${ctx.companyName}`);
        const hardRequirement = mustIncludeCompany && companyName
            ? `Every option **must mention** the company name "${companyName}" explicitly (e.g., "at ${companyName}" or "${companyName} seeks...").`
            : `Do not add or fabricate company names.`;

        const prompt = [
            `Rewrite the following ${input.field} item.`,
            `Produce **three distinct options** that improve clarity, inclusivity, and parallel structure while preserving factual meaning.`,
            `Do not add benefits/salary/company claims not present in context.`,
            hardRequirement,
            `Each option must have ${input.style?.numParagraphs} paragraph(s). `,
            `Each option must be self-contained and ready to display in UI.`,
            hints.length ? `STYLE:\n- ${hints.join("\n- ")}` : `STYLE: (none)`,
            ctxLines.length ? `\nCONTEXT:\n${ctxLines.join("\n")}` : ``,
            `\nITEM:\n${input.value}`,
        ].join("\n");

        try {
            const completion = await this.client.chat.completions.create({
                model: this.model,
                response_format: {
                    type: "json_schema",
                    json_schema: { name: "JobRewriteItemMulti", schema, strict: true },
                },
                messages: [
                    { role: "system", content: "You are a precise job-content editor. Return only JSON that matches the schema." },
                    { role: "user", content: prompt },
                ],
            }, { signal: ac.signal });

            const raw = completion.choices[0]?.message?.content ?? "{}";
            let parsed: any;
            try {
                parsed = JSON.parse(raw);
            } catch {
                const error: any = new Error("Model returned non-JSON");
                error.status = 500;
                throw error;
            }

            const usage = (completion as any).usage ?? {};
            return {
                field: input.field,
                options: parsed.options as string[],
                meta: {
                    model: this.model,
                    promptTokens: usage.prompt_tokens ?? usage.input_tokens,
                    completionTokens: usage.completion_tokens ?? usage.output_tokens,
                    totalTokens: usage.total_tokens ??
                        (typeof usage.prompt_tokens === "number" && typeof usage.completion_tokens === "number"
                            ? usage.prompt_tokens + usage.completion_tokens
                            : undefined),
                    finishReason: completion.choices?.[0]?.finish_reason,
                },
            };
        } finally {
            clearTimeout(t);
        }
    }

    async embed(text: string): Promise<{ vector: number[]; model: string }> {

        const normalized = text.replace(/\s+/g, " ").trim();
        const res = await this.client.embeddings.create({
            model: "text-embedding-3-small",
            input: normalized,
        });
        return { vector: res.data[0].embedding as number[], model: res.model };
    }
}
