using CompanyApi.Application.Queries;
using CompanyApi.Infrastructure.Data;
using CompanyApi.Infrastructure.Data.Entities;
using CompanyApi.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Shouldly;
using System.Security.Claims;

namespace CompanyApi.Tests.Unit.Queries;

[Trait("Category", "Unit")]
public class CompanyQueryServiceTests : IAsyncLifetime
{
    private CompanyDbContext _context = null!;
    private CompanyQueryService _sut = null!;
    private Industry _industry = null!;

    public async Task InitializeAsync()
    {
        (_context, _industry) = await TestDbContextFactory.CreateWithIndustryAsync();
        _sut = new CompanyQueryService(_context);
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListAsync_AdminUser_ShouldReturnAllCompanies()
    {
        _context.Companies.Add(TestDbContextFactory.CreateCompany(_industry, "Corp A", "a@test.com"));
        _context.Companies.Add(TestDbContextFactory.CreateCompany(_industry, "Corp B", "b@test.com"));
        await _context.SaveChangesAsync();

        var httpContext = CreateHttpContext(["/Admins"]);

        var result = await _sut.ListAsync(httpContext, CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_CompanyAdmin_ShouldReturnOnlyOwnCompanies()
    {
        var ownUId = Guid.NewGuid();
        _context.Companies.Add(TestDbContextFactory.CreateCompany(_industry, "Own Corp", "own@test.com", uid: ownUId));
        _context.Companies.Add(TestDbContextFactory.CreateCompany(_industry, "Other Corp", "other@test.com"));
        await _context.SaveChangesAsync();

        var httpContext = CreateHttpContext([$"/Companies/{ownUId}/CompanyAdmins"]);

        var result = await _sut.ListAsync(httpContext, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Own Corp");
    }

    [Fact]
    public async Task ListAsync_UserWithNoGroups_ShouldReturnAllCompanies()
    {
        _context.Companies.Add(TestDbContextFactory.CreateCompany(_industry, "Corp", "a@test.com"));
        await _context.SaveChangesAsync();

        var httpContext = CreateHttpContext([]);
        var result = await _sut.ListAsync(httpContext, CancellationToken.None);

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ListAsync_MultipleGroupPaths_ShouldReturnMatchingCompanies()
    {
        var uid1 = Guid.NewGuid();
        var uid2 = Guid.NewGuid();
        _context.Companies.Add(TestDbContextFactory.CreateCompany(_industry, "Corp 1", "c1@test.com", uid: uid1));
        _context.Companies.Add(TestDbContextFactory.CreateCompany(_industry, "Corp 2", "c2@test.com", uid: uid2));
        _context.Companies.Add(TestDbContextFactory.CreateCompany(_industry, "Corp 3", "c3@test.com"));
        await _context.SaveChangesAsync();

        var httpContext = CreateHttpContext([$"/Companies/{uid1}/Recruiters", $"/Companies/{uid2}/CompanyAdmins"]);

        var result = await _sut.ListAsync(httpContext, CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    private static HttpContext CreateHttpContext(string[] groups)
    {
        var claims = groups.Select(g => new Claim("groups", g)).ToList();
        claims.Add(new Claim("sub", "user-1"));
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return new DefaultHttpContext { User = principal };
    }
}
