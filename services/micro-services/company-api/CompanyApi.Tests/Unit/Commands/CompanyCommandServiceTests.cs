using System.Security.Claims;
using CompanyApi.Application.Commands;
using CompanyApi.Infrastructure.Data;
using CompanyApi.Infrastructure.Data.Entities;
using CompanyApi.Tests.Helpers;
using CompanyAPI.Contracts.Models.Companies.Requests;
using Elkhair.Dev.Common.Dapr;
using JobBoard.IntegrationEvents.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace CompanyApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class CompanyCommandServiceTests : IAsyncLifetime
{
    private readonly IMessageSender _messageSender = Substitute.For<IMessageSender>();
    private CompanyDbContext _context = null!;
    private CompanyCommandService _sut = null!;
    private Industry _industry = null!;

    private readonly ClaimsPrincipal _user = new(new ClaimsIdentity(
    [
        new Claim("sub", "user-123")
    ]));

    public async Task InitializeAsync()
    {
        MapsterSetup.Initialize();
        (_context, _industry) = await TestDbContextFactory.CreateWithIndustryAsync();
        _sut = new CompanyCommandService(_context, _messageSender, Substitute.For<ILogger<CompanyCommandService>>());
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateAsync_ShouldSetStatusToProvisioning()
    {
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "test@corp.com",
            IndustryUId = _industry.UId
        };

        var result = await _sut.CreateAsync(request, _user, CancellationToken.None);

        var saved = await _context.Companies.FirstAsync();
        saved.Status.ShouldBe("Provisioning");
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistCompany()
    {
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "test@corp.com",
            IndustryUId = _industry.UId
        };

        await _sut.CreateAsync(request, _user, CancellationToken.None);

        (await _context.Companies.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_WithCompanyId_ShouldUseProvidedUId()
    {
        var companyId = Guid.NewGuid();
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "test@corp.com",
            IndustryUId = _industry.UId,
            CompanyId = companyId
        };

        await _sut.CreateAsync(request, _user, CancellationToken.None);

        var saved = await _context.Companies.FirstAsync();
        saved.UId.ShouldBe(companyId);
    }

    [Fact]
    public async Task CreateAsync_WithUserId_ShouldSetAuditFields()
    {
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "test@corp.com",
            IndustryUId = _industry.UId,
            UserId = "custom-user"
        };

        await _sut.CreateAsync(request, _user, CancellationToken.None);

        var saved = await _context.Companies.FirstAsync();
        saved.CreatedBy.ShouldBe("custom-user");
        saved.UpdatedBy.ShouldBe("custom-user");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnResponseWithIndustryUId()
    {
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "test@corp.com",
            IndustryUId = _industry.UId
        };

        var result = await _sut.CreateAsync(request, _user, CancellationToken.None);

        result.IndustryUId.ShouldBe(_industry.UId);
        result.Name.ShouldBe("Test Corp");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAllFields()
    {
        var company = TestDbContextFactory.CreateCompany(_industry);
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var request = new UpdateCompanyRequest
        {
            Name = "New Name",
            CompanyEmail = "new@test.com",
            CompanyWebsite = "https://new.com",
            Phone = "555-1234",
            Description = "Desc",
            About = "About",
            EEO = "EEO",
            Founded = new DateTime(2020, 1, 1),
            Size = "50-100",
            Logo = "logo.png",
            IndustryUId = _industry.UId
        };

        await _sut.UpdateAsync(company.UId, request, _user, CancellationToken.None, publishEvent: false);

        var updated = await _context.Companies.FirstAsync();
        updated.Name.ShouldBe("New Name");
        updated.Email.ShouldBe("new@test.com");
        updated.Website.ShouldBe("https://new.com");
        updated.Phone.ShouldBe("555-1234");
        updated.Description.ShouldBe("Desc");
    }

    [Fact]
    public async Task UpdateAsync_ShouldPublishEvent_WhenPublishEventIsTrue()
    {
        var company = TestDbContextFactory.CreateCompany(_industry);
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var request = new UpdateCompanyRequest
        {
            Name = "Updated",
            CompanyEmail = "up@test.com",
            IndustryUId = _industry.UId
        };

        await _sut.UpdateAsync(company.UId, request, _user, CancellationToken.None, publishEvent: true);

        await _messageSender.Received(1).SendEventAsync(
            "rabbitmq.pubsub", "micro.company-updated.v1", "user-123",
            Arg.Is<MicroCompanyUpdatedV1Event>(e => e.Name == "Updated"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotPublishEvent_WhenPublishEventIsFalse()
    {
        var company = TestDbContextFactory.CreateCompany(_industry);
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var request = new UpdateCompanyRequest
        {
            Name = "Updated",
            CompanyEmail = "up@test.com",
            IndustryUId = _industry.UId
        };

        await _sut.UpdateAsync(company.UId, request, _user, CancellationToken.None, publishEvent: false);

        await _messageSender.DidNotReceive().SendEventAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<MicroCompanyUpdatedV1Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateAsync_ShouldSetStatusToActive()
    {
        var company = TestDbContextFactory.CreateCompany(_industry, status: "Provisioning");
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var result = await _sut.ActivateAsync(company.UId, _user, CancellationToken.None);

        result.ShouldBeTrue();
        var activated = await _context.Companies.FirstAsync();
        activated.Status.ShouldBe("Active");
    }

    [Fact]
    public async Task ActivateAsync_WhenCompanyNotFound_ShouldReturnFalse()
    {
        var result = await _sut.ActivateAsync(Guid.NewGuid(), _user, CancellationToken.None);

        result.ShouldBeFalse();
    }
}
