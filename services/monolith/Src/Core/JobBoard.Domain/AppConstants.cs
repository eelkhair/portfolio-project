namespace JobBoard.Domain;

public static class AppConstants
{
    public const int UniqueConstraintSqlError = 2627;
}

public static class UserRoles
{
    public const string LabAdmin = "LabAdmin";
    public const string LabMember = "LabMember";
}

public static class AuthorizationPolicies
{
    public const string AllUsers = "AllUsers";
    public const string Admin = "Admin";
    public const string Member = "Member";

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

    private static readonly Dictionary<string, string> EventTypeToTopic = new()
    {
        ["company.created.v1"] = CompanyCreatedV1,
        ["company.updated.v1"] = CompanyUpdatedV1,
        ["job.created.v1"] = JobCreatedV1,
    };

    public static string GetTopicForEventType(string eventType)
        => EventTypeToTopic.TryGetValue(eventType, out var topic)
            ? topic
            : throw new ArgumentException($"No topic mapped for event type '{eventType}'");
}