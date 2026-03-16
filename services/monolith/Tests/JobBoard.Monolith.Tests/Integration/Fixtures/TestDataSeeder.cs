using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using JobBoard.Infrastructure.Persistence.Context;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Monolith.Tests.Integration.Fixtures;

/// <summary>
/// Reusable seed helpers for integration tests.
/// All methods use the dual-ID pattern (int InternalId + Guid Id) via sequence generation.
/// </summary>
public class TestDataSeeder(TestDatabaseFixture dbFixture)
{
    public async Task<Industry> SeedIndustryAsync(string? name = null)
    {
        await using var ctx = dbFixture.CreateContext();
        name ??= $"Industry-{Guid.NewGuid().ToString()[..8]}";

        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);
        var industry = Industry.Create(name);
        industry.InternalId = internalId;
        industry.Id = id;
        industry.CreatedAt = DateTime.UtcNow;
        industry.CreatedBy = "seed";
        industry.UpdatedAt = DateTime.UtcNow;
        industry.UpdatedBy = "seed";
        ctx.Industries.Add(industry);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return industry;
    }

    public async Task<Company> SeedCompanyAsync(int industryInternalId, string? name = null, string? status = "Provisioning")
    {
        await using var ctx = dbFixture.CreateContext();
        var suffix = Guid.NewGuid().ToString()[..8];
        name ??= $"Company-{suffix}";

        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Company), CancellationToken.None);
        var company = Company.Create(new CompanyInput(
            InternalId: internalId,
            Id: id,
            Name: name,
            Email: $"company-{suffix}@test.com",
            Status: status!,
            IndustryId: industryInternalId,
            Website: "https://test.com",
            CreatedAt: DateTime.UtcNow,
            CreatedBy: "seed"));
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return company;
    }

    public async Task<Company> SeedActivatedCompanyAsync(int industryInternalId, string? name = null)
    {
        return await SeedCompanyAsync(industryInternalId, name, "Active");
    }

    public async Task<User> SeedUserAsync(string? firstName = null, string? lastName = null, string? externalId = null)
    {
        await using var ctx = dbFixture.CreateContext();
        var suffix = Guid.NewGuid().ToString()[..8];
        firstName ??= "Test";
        lastName ??= "User";
        externalId ??= $"ext-{suffix}";

        // If a user with this externalId already exists (e.g. auto-created by UserContextDecorator), return it
        var existing = await ctx.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId);
        if (existing is not null)
            return existing;

        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(User), CancellationToken.None);
        var user = User.Create(firstName, lastName, $"user-{suffix}@test.com", externalId, id, internalId,
            DateTime.UtcNow, "seed");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return user;
    }

    public async Task<UserCompany> SeedUserCompanyAsync(int userId, int companyId)
    {
        await using var ctx = dbFixture.CreateContext();
        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(UserCompany), CancellationToken.None);
        var uc = UserCompany.Create(userId, companyId, internalId, id, DateTime.UtcNow, "seed");
        ctx.UserCompanies.Add(uc);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return uc;
    }

    public async Task<Job> SeedJobAsync(int companyInternalId, string? title = null)
    {
        await using var ctx = dbFixture.CreateContext();
        var suffix = Guid.NewGuid().ToString()[..8];
        title ??= $"Job-{suffix}";

        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Job), CancellationToken.None);
        var job = Job.Create(new JobInput
        {
            Title = title,
            Location = "Remote",
            AboutRole = "Test role description for integration tests",
            SalaryRange = "$80k-$120k",
            JobType = JobType.FullTime,
            CompanyId = companyInternalId,
            Responsibilities = ["Responsibility 1", "Responsibility 2"],
            Qualifications = ["Qualification 1", "Qualification 2"],
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed",
            InternalId = internalId,
            UId = id
        });
        ctx.Jobs.Add(job);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return job;
    }

    public async Task<Draft> SeedDraftAsync(Guid companyId, string? contentJson = null)
    {
        await using var ctx = dbFixture.CreateContext();
        contentJson ??= """{"title":"Test Draft","aboutRole":"Test role"}""";

        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Draft), CancellationToken.None);
        var draft = Draft.Create(companyId, contentJson, internalId, id);
        draft.CreatedAt = DateTime.UtcNow;
        draft.CreatedBy = "seed";
        draft.UpdatedAt = DateTime.UtcNow;
        draft.UpdatedBy = "seed";
        ctx.Drafts.Add(draft);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return draft;
    }

    public async Task<Resume> SeedResumeAsync(int userId, string? parsedContent = null, bool isDefault = false)
    {
        await using var ctx = dbFixture.CreateContext();
        var suffix = Guid.NewGuid().ToString()[..8];

        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Resume), CancellationToken.None);
        var resume = Resume.Create(new ResumeInput
        {
            UserId = userId,
            FileName = $"resume-{suffix}.pdf",
            OriginalFileName = $"MyResume-{suffix}.pdf",
            ContentType = "application/pdf",
            FileSize = 1024,
            ParsedContent = parsedContent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed",
            InternalId = internalId,
            UId = id
        });
        if (isDefault)
            resume.SetAsDefault();
        ctx.Resumes.Add(resume);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return resume;
    }

    public async Task<UserProfile> SeedUserProfileAsync(int userId)
    {
        await using var ctx = dbFixture.CreateContext();
        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(UserProfile), CancellationToken.None);
        var profile = UserProfile.Create(new UserProfileInput
        {
            UserId = userId,
            Phone = "+1234567890",
            LinkedIn = "https://linkedin.com/in/test",
            About = "Test user profile",
            Skills = ["C#", "TypeScript"],
            PreferredLocation = "Remote",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed",
            InternalId = internalId,
            UId = id
        });
        ctx.UserProfiles.Add(profile);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return profile;
    }

    public async Task<JobApplication> SeedApplicationAsync(int jobInternalId, int userInternalId)
    {
        await using var ctx = dbFixture.CreateContext();
        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(JobApplication), CancellationToken.None);
        var application = JobApplication.Create(new JobApplicationInput
        {
            JobId = jobInternalId,
            UserId = userInternalId,
            CoverLetter = "Test cover letter",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed",
            InternalId = internalId,
            UId = id
        });
        ctx.JobApplications.Add(application);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return application;
    }
}
