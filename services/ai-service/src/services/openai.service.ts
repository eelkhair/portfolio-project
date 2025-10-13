import {OpenAI} from "openai";
import {JobGenRequest, JobGenResponse} from "../schemas/job-generate.js";
import {SYSTEM_PROMPT, userPrompt} from "../prompts/job-generate.js";

export class OpenAIService{
    private client: OpenAI;
    private model: string;
    constructor(opts: { apiKey: string; model: string }) {
        this.client = new OpenAI({ apiKey: opts.apiKey });
        this.model = opts.model;
    }
    async rewrite(prompt: string, { timeoutMs = 15000 } = {}) {
        const ac = new AbortController();
        const t = setTimeout(() => ac.abort(), timeoutMs);
        try {
            const res = await this.client.chat.completions.create({
                model: this.model,
                messages: [{ role: "user", content: prompt }],
            }, { signal: ac.signal });
            const text = res.choices[0]?.message?.content ?? "";
            return { text };
        } finally {
            clearTimeout(t);
        }
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
                // notes & location are required in strict mode; allow empty string for both
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
}
