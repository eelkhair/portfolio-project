using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Dapr.Client;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;
using UserApi.Application.Commands.Interfaces;
using UserAPI.Contracts.Models.Events;
using UserAPI.Contracts.Models.Requests;

namespace UserApi.Features.Events;

public class ProvisionUserTopic(
    ActivitySource activitySource,
    DaprClient client,
    IKeycloakCommandService keycloakService,
    IMessageSender sender,
    ICompanyCommandService commandService,
    ILogger<ProvisionUserTopic> logger)
    : Endpoint<EventDto<ProvisionUserEvent>>
{
    public override void Configure()
    {
        Post("events/company/create");
        AllowAnonymous();
        Options(o => o.WithTopic(PubSubNames.RabbitMq, "company.created"));
    }

    public override async Task HandleAsync(EventDto<ProvisionUserEvent> request, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new
        {
            request.IdempotencyKey,
            request.UserId,
            request.Data.Email,
            request.Data.CompanyUId,
            request.Data.CompanyName
        });

        logger.LogInformation("Starting user provisioning workflow for {Email}", request.Data.Email);

        using var spanIdempotency =
            activitySource.StartActivity("provision.user.idempotency");

        var stateKey = $"{IdempotencyOptions.Prefix}{request.IdempotencyKey}";
        var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: ct);

        if (existing is not null)
        {
            logger.LogInformation("Skipping provisioning. Idempotency key already processed");
            await Send.OkAsync(request, ct);
            return;
        }

        await client.SaveStateAsync(
            StateStores.Redis,
            stateKey,
            "processing",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["ttlInSeconds"] = IdempotencyOptions.PendingTTLSeconds.ToString() },
            cancellationToken: ct);


        try
        {
            using var spanKeycloak =
                activitySource.StartActivity("provision.user.keycloak", ActivityKind.Client);

            logger.LogInformation("Provisioning user in Keycloak…");

            var (keycloakUser, keycloakGroup) = await keycloakService.ProvisionUserAsync(request.Data, ct);

            if (keycloakUser is null || keycloakGroup is null)
            {
                throw new InvalidOperationException("Keycloak provisioning returned null values.");
            }

            spanKeycloak?.SetTag("keycloak.user.id", keycloakUser.Id);
            spanKeycloak?.SetTag("keycloak.group.id", keycloakGroup.Id);


            using var spanDb =
                activitySource.StartActivity("provision.user.persistence");

            logger.LogInformation("Persisting new user and company records…");


            var userId = await commandService.CreateUser(new CreateUserRequest
            {
                KeycloakId = keycloakUser.Id!,
                FirstName = keycloakUser.FirstName!,
                LastName = keycloakUser.LastName!,
                Email = keycloakUser.Email!,
                UId = request.Data.UId

            }, request.UserId, ct);

            var companyId = await commandService.CreateCompany(new CreateCompanyRequest
            {
                KeycloakGroupId = keycloakGroup.Id!,
                Name = keycloakGroup.Name!,
                UId = request.Data.CompanyUId,
            }, request.UserId, ct);

            await commandService.AddUserToCompany(userId, companyId, request.UserId, request.Data.UserCompanyUId, ct);

            spanDb?.SetTag("db.user.id", userId);
            spanDb?.SetTag("db.company.id", companyId);


            request.Data.KeycloakGroupId = keycloakGroup.Id!;
            request.Data.KeycloakUserId = keycloakUser.Id!;

            logger.LogInformation("Emitting success event for user provisioning");
            await sender.SendEventAsync(
                PubSubNames.RabbitMq,
                "provision.user.success",
                request.UserId,
                request.Data,
                ct);
        }
        catch (Exception ex)
        {
            using var spanError =
                activitySource.StartActivity("provision.user.error");

            spanError?.SetTag("exception", true);
            spanError?.SetTag("exception.message", ex.Message);

            logger.LogError(ex, "Unhandled error while provisioning user");

            await sender.SendEventAsync(
                PubSubNames.RabbitMq,
                "provision.user.error",
                request.UserId,
                ex.Message,
                ct);

            throw;
        }


        using (activitySource.StartActivity("provision.user.finalize"))
        {
            logger.LogInformation("Marking provisioning workflow as completed");

            await client.SaveStateAsync(
                StateStores.Redis,
                stateKey,
                "done",
                metadata: new Dictionary<string, string>
(StringComparer.Ordinal)
                {
                    ["ttlInSeconds"] = IdempotencyOptions.CompletedTTLSeconds.ToString()
                },
                cancellationToken: ct);
        }

        logger.LogInformation("User {Email} provisioned successfully", request.Data.Email);

        await Send.OkAsync(request, ct);
    }
}

internal static class IdempotencyOptions
{
    public const string Prefix = "Provisioned:";
    public const int PendingTTLSeconds = 120;
    public const int CompletedTTLSeconds = 7 * 24 * 3600;
}
