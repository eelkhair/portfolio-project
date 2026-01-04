import {z} from "zod";

export const JobGenRequest = z.object({
    brief: z.string().min(10),
    roleLevel: z.enum(["junior","mid","senior", "staff","principal"]).default("mid"),
    tone: z.enum(["neutral","concise","friendly"]).default("neutral"),
    maxBullets: z.number().int().min(3).max(8).default(6),
    companyName: z.string().optional(),
    teamName: z.string().optional(),
    location: z.string().optional(),
    titleSeed: z.string().optional(),
    techStackCSV: z.string().optional(),
    mustHavesCSV: z.string().optional(),
    niceToHavesCSV: z.string().optional(),
    benefits: z.string().optional()
});

export const JobGenResponse = z.object({
    title: z.string().min(6).max(80),
    aboutRole: z.string().min(60).max(1500),
    responsibilities: z.array(z.string().min(3)).min(3).max(8),
    qualifications: z.array(z.string().min(3)).min(3).max(8),
    notes: z.string().max(600),
    location: z.string().optional(),
    metadata: z.object({
        roleLevel: z.enum(["junior","mid","senior", "staff", "principal"]).default("mid"),
        tone: z.enum(["neutral","concise","friendly"])
    }),
    draftId: z.string().optional(),
});
