import {OpenAI} from "openai";

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
}
