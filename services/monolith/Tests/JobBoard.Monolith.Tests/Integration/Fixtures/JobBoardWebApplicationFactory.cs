using JobBoard.API.Infrastructure.SignalR.CompanyActivation;
using JobBoard.API.Infrastructure.SignalR.ResumeParse;
using JobBoard.Application.Actions.Public;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Storage;
using JobBoard.Mcp.Common;
using JobBoard.Infrastructure.Diagnostics.Observability;
using JobBoard.Infrastructure.Persistence.Context;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Settings;
using JobBoard.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace JobBoard.Monolith.Tests.Integration.Fixtures;

public class JobBoardWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly TestDatabaseFixture _dbFixture;

    public JobBoardWebApplicationFactory(TestDatabaseFixture dbFixture)
    {
        // Must be set before the host is created so Program.cs sees Testing environment
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        _dbFixture = dbFixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Monolith"] = _dbFixture.ConnectionString,
                ["Keycloak:Authority"] = "https://auth.test.com/realms/test",
                ["Keycloak:Audience"] = "jobboard-api",
                ["Keycloak:SwaggerClientId"] = "swagger-test",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace DbContext with Testcontainers connection
            services.RemoveAll<DbContextOptions<JobBoardDbContext>>();
            services.RemoveAll<JobBoardDbContext>();
            services.AddDbContext<JobBoardDbContext>(options =>
                options.UseSqlServer(_dbFixture.ConnectionString));

            services.RemoveAll<IOutboxMessageProcessor>();
            services.AddTransient(_ => Substitute.For<IOutboxMessageProcessor>());

            services.RemoveAll<IAiServiceClient>();
            services.AddScoped(_ =>
            {
                var mock = Substitute.For<IAiServiceClient>();
                mock.GetProvider(Arg.Any<CancellationToken>())
                    .Returns(new ProviderSettings { Provider = "openai", Model = "gpt-4.1-mini" });
                mock.GetApplicationMode(Arg.Any<CancellationToken>())
                    .Returns(new ApplicationModeDto { IsMonolith = true });
                mock.GenerateDraft(Arg.Any<Guid>(), Arg.Any<DraftGenRequest>(), Arg.Any<CancellationToken>())
                    .Returns(new DraftGenResponse { Title = "Test Draft", DraftId = "draft-1", AboutRole = "Test role" });
                mock.RewriteItem(Arg.Any<DraftItemRewriteRequest>(), Arg.Any<CancellationToken>())
                    .Returns(new DraftRewriteResponse { Field = "title", Options = ["Option A"] });
                mock.GetSimilarJobs(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(new List<JobCandidate>());
                mock.SearchJobs(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(new List<JobCandidate>());
                mock.GetMatchingJobsForResumeAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(new List<JobCandidate>());
                mock.ReEmbedAllJobs(Arg.Any<CancellationToken>())
                    .Returns(new ReEmbedAllJobsResponse(0));
                mock.UpdateProvider(Arg.Any<UpdateProviderRequest>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask);
                mock.UpdateApplicationMode(Arg.Any<ApplicationModeDto>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask);
                return mock;
            });

            // Stub SignalR notifiers
            services.RemoveAll<ICompanyActivationNotifier>();
            services.AddSingleton(Substitute.For<ICompanyActivationNotifier>());

            services.RemoveAll<IResumeParseNotifier>();
            services.AddSingleton(Substitute.For<IResumeParseNotifier>());

            // Stub blob storage
            services.RemoveAll<IBlobStorageService>();
            services.AddScoped(_ =>
            {
                var mock = Substitute.For<IBlobStorageService>();
                mock.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(ci => $"https://blob.test/{ci.ArgAt<string>(0)}/{ci.ArgAt<string>(1)}");
                mock.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(new BlobDownloadResponse(new MemoryStream("fake-pdf-content"u8.ToArray()), "application/pdf"));
                mock.DeleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask);
                return mock;
            });

            // Register test auth handler and force it as default scheme
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });

            // Override InternalOrJwt policy to accept TestScheme
            services.AddAuthorizationBuilder()
                .AddPolicy(AuthorizationPolicies.InternalOrJwt, policy =>
                    policy.AddAuthenticationSchemes(
                            JwtBearerDefaults.AuthenticationScheme,
                            "InternalApiKey",
                            TestAuthHandler.SchemeName)
                        .RequireAuthenticatedUser());

            // Ensure observability services are registered (they may not be in test env)
            services.TryAddSingleton<IActivityFactory, ActivitySourceFactory>();
            services.TryAddSingleton<IMetricsService, MetricsService>();
            services.TryAddScoped<IUnitOfWorkEvents, UnitOfWorkEvents>();
        });
    }
}
