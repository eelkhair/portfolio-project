using Elkhair.Dev.Common.Domain.Constants;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Domain.Constants;

[Trait("Category", "Unit")]
public class RecordStatusesTests
{
    [Fact]
    public void Active_ShouldBeActive()
    {
        RecordStatuses.Active.ShouldBe("Active");
    }

    [Fact]
    public void Archived_ShouldBeArchived()
    {
        RecordStatuses.Archived.ShouldBe("Archived");
    }

    [Fact]
    public void Deleted_ShouldBeDeleted()
    {
        RecordStatuses.Deleted.ShouldBe("Deleted");
    }
}

[Trait("Category", "Unit")]
public class PubSubNamesTests
{
    [Fact]
    public void Default_ShouldBeRabbitMqPubSub()
    {
        PubSubNames.Default.ShouldBe("rabbitmq.pubsub");
    }

    [Fact]
    public void RabbitMq_ShouldBeRabbitMqPubSub()
    {
        PubSubNames.RabbitMq.ShouldBe("rabbitmq.pubsub");
    }

    [Fact]
    public void Redis_ShouldBeRedisPubSub()
    {
        PubSubNames.Redis.ShouldBe("redis.pubsub");
    }

    [Fact]
    public void Default_ShouldEqualRabbitMq()
    {
        PubSubNames.Default.ShouldBe(PubSubNames.RabbitMq);
    }
}

[Trait("Category", "Unit")]
public class EventTypesTests
{
    [Fact]
    public void CompanyCreated_ShouldBeCorrect()
    {
        EventTypes.CompanyCreated.ShouldBe("company.created");
    }

    [Fact]
    public void CompanyUpdated_ShouldBeCorrect()
    {
        EventTypes.CompanyUpdated.ShouldBe("company.updated");
    }
}

[Trait("Category", "Unit")]
public class TopicNamesTests
{
    [Fact]
    public void CompanyCreate_ShouldBeCorrect()
    {
        TopicNames.CompanyCreate.ShouldBe("company.created");
    }

    [Fact]
    public void CompanyUpdate_ShouldBeCorrect()
    {
        TopicNames.CompanyUpdate.ShouldBe("company.updated");
    }
}

[Trait("Category", "Unit")]
public class SecretStoreNamesTests
{
    [Fact]
    public void Local_ShouldBeVault()
    {
        SecretStoreNames.Local.ShouldBe("vault");
    }
}

[Trait("Category", "Unit")]
public class StateStoresTests
{
    [Fact]
    public void Redis_ShouldBeCorrect()
    {
        AH.Metadata.Domain.Constants.StateStores.Redis.ShouldBe("statestore.redis");
    }
}
