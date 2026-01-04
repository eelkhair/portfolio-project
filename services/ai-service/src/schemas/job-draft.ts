import {z} from "zod";

export const JobDraft = z.object({
    title: z.string().optional(),
    aboutRole: z.string().optional(),
    responsibilities: z.array(z.string()).optional(),
    qualifications: z.array(z.string()).optional(),
    notes: z.string().optional(),
    location: z.string().optional(),
    metadata: z.object({
        roleLevel: z.enum(["junior","mid","senior", "staff", "principal"]).default("mid").optional(),
        tone: z.enum(["neutral","concise","friendly"]).default("neutral").optional(),
    }),
    jobType: z.enum(["fullTime", "partTime", "internship", "contract", "temporary", "other"]).optional(),
    salaryRange: z.string().optional(),
    id: z.string().optional(),
});
