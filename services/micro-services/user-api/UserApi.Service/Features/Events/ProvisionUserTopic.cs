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
    IAuth0CommandService aut0Service,
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
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = IdempotencyOptions.PendingTTLSeconds.ToString() },
            cancellationToken: ct);


        try
        {
            using var spanAuth0 =
                activitySource.StartActivity("provision.user.auth0", ActivityKind.Client);

            logger.LogInformation("Provisioning user in Auth0…");

            var (auth0User, auth0Company) = await aut0Service.ProvisionUserAsync(request.Data, ct);

            if (auth0User is null || auth0Company is null)
            {
                throw new InvalidOperationException("Auth0 provisioning returned null values.");
            }

            spanAuth0?.SetTag("auth0.user.id", auth0User.UserId);
            spanAuth0?.SetTag("auth0.company.id", auth0Company.Id);

        
            using var spanDb =
                activitySource.StartActivity("provision.user.persistence");

            logger.LogInformation("Persisting new user and company records…");
            

            var userId = await commandService.CreateUser(new CreateUserRequest
            {
                Auth0Id = auth0User.UserId,
                FirstName = auth0User.FirstName,
                LastName = auth0User.LastName,
                Email = auth0User.Email,
                UId = request.Data.UId
                
            }, request.UserId, ct);

            var companyId = await commandService.CreateCompany(new CreateCompanyRequest
            {
                Auth0OrganizationId = auth0Company.Id,
                Name = auth0Company.DisplayName,
                UId = request.Data.CompanyUId,
            }, request.UserId, ct);

            await commandService.AddUserToCompany(userId, companyId, request.UserId, request.Data.UserCompanyUId, ct);

            spanDb?.SetTag("db.user.id", userId);
            spanDb?.SetTag("db.company.id", companyId);

            
            request.Data.Auth0OrganizationId = auth0Company.Id;
            request.Data.Auth0UserId = auth0User.UserId;

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
