namespace JobBoard.Domain;

public static class AppConstants
{
    public const int UniqueConstraintSqlError = 2627;
}

public static class UserRoles
{
    public const string Admin = "Admins";
    public const string Recruiter = "Recruiters";
    public const string CompanyAdmin = "CompanyAdmins";
    public const string Applicant = "Applicants";
}

public static class AuthorizationPolicies
{
    public const string AllUsers = "AllUsers";
    public const string Admin = "Admin";
    public const string Recruiter = "Recruiter";
    public const string InternalOrJwt = "InternalOrJwt";
}

public static class PubSubNames
{
    public const string Default = "rabbitmq.pubsub";
    public const string RabbitMq = "rabbitmq.pubsub";
    public const string Redis = "redis.pubsub";
}

public static class MonolithTopicNames
{
    public const string CompanyCreatedV1 = "monolith.company-created.v1";
    public const string CompanyUpdatedV1 = "monolith.company-updated.v1";
    public const string JobCreatedV1 = "monolith.job-created.v1";
    public const string ResumeUploadedV1 = "monolith.resume-uploaded.v1";
    public const string ResumeParsedV1 = "monolith.resume-parsed.v1";
    public const string ResumeDeletedV1 = "monolith.resume-deleted.v1";
    public const string DraftSavedV1 = "monolith.draft-saved.v1";
    public const string DraftDeletedV1 = "monolith.draft-deleted.v1";

    private static readonly Dictionary<string, string> EventTypeToTopic = new()
    {
        ["company.created.v1"] = CompanyCreatedV1,
        ["company.updated.v1"] = CompanyUpdatedV1,
        ["job.created.v1"] = JobCreatedV1,
        ["resume.uploaded.v1"] = ResumeUploadedV1,
        ["resume.parsed.v1"] = ResumeParsedV1,
        ["resume.deleted.v1"] = ResumeDeletedV1,
        ["draft.saved.v1"] = DraftSavedV1,
        ["draft.deleted.v1"] = DraftDeletedV1,
    };

    public static string GetTopicForEventType(string eventType)
        => EventTypeToTopic.TryGetValue(eventType, out var topic)
            ? topic
            : throw new ArgumentException($"No topic mapped for event type '{eventType}'");
}

public static class MicroTopicNames
{
    public const string DraftSavedV1 = "micro.draft-saved.v1";
    public const string DraftDeletedV1 = "micro.draft-deleted.v1";
}