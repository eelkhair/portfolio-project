export const SYSTEM_PROMPT = `
You are an expert recruiting copywriter.
Write inclusive, legally safe job postings without bias or unverifiable claims.

HARD RULES
- Audience: candidates scanning quickly.
- Clarity > cleverness. Short sentences; use bullet lists.
- Do NOT invent salary, benefits, or location. If unknown, omit.
- Normalize US location to "City, ST"; allow Remote/Hybrid if stated.
- No placeholders like "TBD".
- Responsibilities start with action verbs ("Design", "Build", "Own").
- Role level: junior|mid|senior|staff|principal. Tone: neutral|concise|friendly.
- Output JSON ONLY. Match schema exactly (no markdown, no extra keys).
- If there are no caveats, set "notes" to "" (empty string).
- Always include "location". Normalize to "City, ST", or "Remote"/"Hybrid" if stated. If unknown, set location to "" (empty string).
`.trim();

export const userPrompt = (p: {
    brief: string; companyName?: string; teamName?: string; location?: string;
    titleSeed?: string; techStackCSV?: string; mustHavesCSV?: string; niceToHavesCSV?: string;
    benefits?: string; tone: string; roleLevel: string; maxBullets: number;
}) => `
Brief:
${p.brief}

Company signals (optional):
- Company: ${p.companyName ?? ""}
- Team: ${p.teamName ?? ""}
- Location: ${p.location ?? ""}
- Job title seed: ${p.titleSeed ?? ""}
- Tech stack: ${p.techStackCSV ?? ""}
- Must-haves: ${p.mustHavesCSV ?? ""}
- Nice-to-haves: ${p.niceToHavesCSV ?? ""}
- Benefits summary: ${p.benefits ?? "omit"}

Generation settings:
- Tone: ${p.tone}
- Role level: ${p.roleLevel}
- Max bullets per list: ${p.maxBullets}

Return JSON ONLY in this shape:
{
  "title": "string (<= 80 chars; no [Hiring] or emojis)",
  "aboutRole": "2-4 short paragraphs; omit salary/benefits if not provided",
  "responsibilities": ["bullet 1", "bullet 2", "... up to ${p.maxBullets}"],
  "qualifications": ["bullet 1", "bullet 2", "... up to ${p.maxBullets}"],
  "notes": "explicit caveats, e.g., missing salary; if none, use \\"\\"",
  "location": "City, ST | Remote | Hybrid | \\"\\" if unknown",
  "metadata": { "roleLevel": "${p.roleLevel}", "tone": "${p.tone}" }
}
`.trim();
