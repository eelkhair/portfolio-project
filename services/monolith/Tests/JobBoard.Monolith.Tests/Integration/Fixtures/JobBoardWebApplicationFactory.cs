using Dapr.Client;
using JobBoard.API.Infrastructure.SignalR.CompanyActivation;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Users;
using JobBoard.Infrastructure.Diagnostics.Observability;
using JobBoard.Infrastructure.Persistence.Context;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.AspNetCore.Authentication;
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
                ["Auth0:Domain"] = "test.auth0.com",
                ["Auth0:Audience"] = "https://test-api",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove Dapr-related hosted services (FeatureFlagWatcher)
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var svc in hostedServices)
            {
                if (svc.ImplementationType?.Name.Contains("FeatureFlagWatcher") == true
                    || svc.ImplementationFactory?.Method.ReturnType.Name.Contains("FeatureFlagWatcher") == true)
                {
                    services.Remove(svc);
                }
            }

            // Replace DbContext with Testcontainers connection
            services.RemoveAll<DbContextOptions<JobBoardDbContext>>();
            services.RemoveAll<JobBoardDbContext>();
            services.AddDbContext<JobBoardDbContext>(options =>
                options.UseSqlServer(_dbFixture.ConnectionString));

            // Stub Dapr services
            services.RemoveAll<DaprClient>();
            services.AddSingleton(Substitute.For<DaprClient>());

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
                mock.ListDrafts(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(new List<DraftResponse> { new() { Id = "draft-1", Title = "Test Draft" } });
                mock.RewriteItem(Arg.Any<DraftItemRewriteRequest>(), Arg.Any<CancellationToken>())
                    .Returns(new DraftRewriteResponse { Field = "title", Options = ["Option A"] });
                return mock;
            });

            // Stub SignalR notifiers
            services.RemoveAll<ICompanyActivationNotifier>();
            services.AddSingleton(Substitute.For<ICompanyActivationNotifier>());

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

            // Ensure observability services are registered (they may not be in test env)
            services.TryAddSingleton<IActivityFactory, ActivitySourceFactory>();
            services.TryAddSingleton<IMetricsService, MetricsService>();
            services.TryAddScoped<IUnitOfWorkEvents, UnitOfWorkEvents>();
        });
    }
}
