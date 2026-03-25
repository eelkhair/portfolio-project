using System.Security.Claims;
using JobApi.Application;
using JobApi.Infrastructure.Data;
using JobApi.Tests.Helpers;
using JobAPI.Contracts.Models.Companies.Requests;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace JobApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class CompanyCommandServiceTests : IAsyncLifetime
{
    private JobDbContext _context = null!;
    private CompanyCommandService _sut = null!;

    private readonly ClaimsPrincipal _user = new(new ClaimsIdentity(
    [
        new Claim("sub", "user-123")
    ]));

    public Task InitializeAsync()
    {
        _context = TestDbContextFactory.Create();
        _sut = new CompanyCommandService(_context);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateCompanyAsync_ShouldPersistCompany()
    {
        var companyUId = Guid.NewGuid();
        var request = new CreateCompanyRequest { Name = "Acme Corp", UId = companyUId };

        await _sut.CreateCompanyAsync(request, _user, CancellationToken.None);

        var saved = await _context.Companies.FirstAsync();
        saved.Name.ShouldBe("Acme Corp");
        saved.UId.ShouldBe(companyUId);
    }

    [Fact]
    public async Task CreateCompanyAsync_ShouldSetProvidedUId()
    {
        var uid = Guid.NewGuid();
        var request = new CreateCompanyRequest { Name = "Test Inc", UId = uid };

        await _sut.CreateCompanyAsync(request, _user, CancellationToken.None);

        var saved = await _context.Companies.FirstAsync();
        saved.UId.ShouldBe(uid);
    }

    [Fact]
    public async Task CreateCompanyAsync_ShouldSetAuditFields()
    {
        var request = new CreateCompanyRequest { Name = "Audit Corp", UId = Guid.NewGuid() };

        await _sut.CreateCompanyAsync(request, _user, CancellationToken.None);

        var saved = await _context.Companies.FirstAsync();
        saved.CreatedBy.ShouldBe("user-123");
        saved.UpdatedBy.ShouldBe("user-123");
        saved.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task UpdateCompanyAsync_ShouldUpdateName()
    {
        var company = TestDbContextFactory.CreateCompany("Old Name");
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var request = new UpdateCompanyRequest { Name = "New Name" };

        await _sut.UpdateCompanyAsync(company.UId, request, _user, CancellationToken.None);

        var updated = await _context.Companies.FirstAsync();
        updated.Name.ShouldBe("New Name");
    }

    [Fact]
    public async Task UpdateCompanyAsync_WhenCompanyNotFound_ShouldThrow()
    {
        var request = new UpdateCompanyRequest { Name = "Doesn't Matter" };

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.UpdateCompanyAsync(Guid.NewGuid(), request, _user, CancellationToken.None));
    }
}
