// The actual job payload
import {z} from "zod";

export const JobPayload = z.object({
    uId: z.string(),
    companyUId: z.string(),
    companyName: z.string(),
    title: z.string(),
    aboutRole: z.string(),
    location: z.string(),
    jobType: z.number(),
    responsibilities: z.array(z.string()),
    qualifications: z.array(z.string()),
    salaryRange: z.string().nullable().optional(),
    createdAt: z.string(),
    updatedAt: z.string(),
    model: z.string().optional(),
    vector:z.array(z.number()).optional()

});

// The outer "envelope" your admin-api is sending inside CloudEvent.data
export const AdminApiEnvelope = z.object({
    created: z.string(),
    data: JobPayload,                  // 👈 actual job is nested here
    idempotencyKey: z.string(),
    userId: z.string()
});

const CloudEventBase = z.object({
    id: z.string(),
    source: z.string(),
    specversion: z.string(),
    type: z.string(),
    time: z.string().optional(),
    datacontenttype: z.string().optional(),
    pubsubname: z.string().optional(),
    topic: z.string().optional(),
    traceid: z.string().optional(),
    traceparent: z.string().optional(),
    tracestate: z.string().optional()
});

export const JobPublishedCloudEvent = z.union([
    CloudEventBase.extend({ data: JobPayload }),
    CloudEventBase.extend({ data: AdminApiEnvelope })
]);

export function extractJobPayload(ce: z.infer<typeof JobPublishedCloudEvent>): z.infer<typeof JobPayload> {
    const anyData: any = ce.data;
    return anyData?.data && typeof anyData.data === "object" ? anyData.data : anyData;
}
export type JobPayloadType = z.infer<typeof JobPayload>;

// now your function uses the inferred type
export function buildJobText(j: JobPayloadType): string {
    const parts = [
        `Title: ${j.title}`,
        `Company: ${j.companyName}`,
        `Location: ${j.location}`,
        `About: ${j.aboutRole}`,
        j.responsibilities?.length
            ? `Responsibilities:\n- ${j.responsibilities.join("\n- ")}`
            : "",
        j.qualifications?.length
            ? `Qualifications:\n- ${j.qualifications.join("\n- ")}`
            : "",
    ];
    return parts.filter(Boolean).join("\n\n");
}
