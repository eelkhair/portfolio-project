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