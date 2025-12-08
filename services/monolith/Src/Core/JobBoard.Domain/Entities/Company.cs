using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.ValueObjects.Company;
using JobBoard.Domain.Helpers;

namespace JobBoard.Domain.Entities;

public class Company : BaseAuditableEntity
{
    protected Company()
    {
        Name = string.Empty;
        Email = string.Empty;
        Status = string.Empty;
    }

    private Company(string name, string email, string status)
    {
        Name = name;
        Email = email;
        Status = status;
    }

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Website { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string? About { get; private set; }
    public string? EEO { get; private set; }
    public DateTime? Founded { get; private set; }
    public string? Size { get; private set; }
    public string? Logo { get; private set; }
    public string Status { get; private set; }

    public string? ExternalId { get; private set; } = string.Empty;
    public int IndustryId { get; private set; }
    public Industry Industry { get; private set; } = null!;
    

    private readonly List<Job> _jobs = [];
    public IReadOnlyCollection<Job> Jobs => _jobs.AsReadOnly();

    public void AddJob(Job job)
    {
        if (job is null)
        {
            throw new DomainException(
                "Company.NullJob",
                [new Error("Company.NullJob", "Job cannot be null.")]
            );
        }

        job.SetCompany(InternalId);
        _jobs.Add(job);
    }
    
    public void SetName(string name) =>
        Name = CompanyName.Create(name).Ensure<CompanyName, string>("Company.InvalidName")!;

    public void SetEmail(string email) =>
        Email = CompanyEmail.Create(email).Ensure<CompanyEmail, string>("Company.InvalidEmail")!;

    public void SetStatus(string status) =>
        Status = CompanyStatus.Create(status).Ensure<CompanyStatus, string>("Company.InvalidStatus")!;

    public void SetExternalId(string externalId) =>
        ExternalId = CompanyExternalId.Create(externalId).Ensure<CompanyExternalId, string?>("Company.InvalidExternalId")!;
    public void SetDescription(string? description) =>
        Description = CompanyDescription.Create(description)
            .Ensure<CompanyDescription, string?>("Company.InvalidDescription");

    public void SetWebsite(string? website) =>
        Website = CompanyWebsite.Create(website)
            .Ensure<CompanyWebsite, string?>("Company.InvalidWebsite");

    public void SetAbout(string? about) =>
        About = CompanyAbout.Create(about)
            .Ensure<CompanyAbout, string?>("Company.InvalidAbout");

    public void SetEEO(string? eeo) =>
        EEO = CompanyEEO.Create(eeo)
            .Ensure<CompanyEEO, string?>("Company.InvalidEEO");

    public void SetPhone(string? phone) =>
        Phone = CompanyPhone.Create(phone)
            .Ensure<CompanyPhone, string?>("Company.InvalidPhone");

    public void SetLogo(string? logo) =>
        Logo = CompanyLogo.Create(logo)
            .Ensure<CompanyLogo, string?>("Company.InvalidLogo");

    public void SetFounded(DateTime? founded) =>
        Founded = CompanyFounded.Create(founded)
            .Ensure<CompanyFounded, DateTime?>("Company.InvalidFoundedDate");

    public void SetSize(string? size) =>
        Size = CompanySize.Create(size)
            .Ensure<CompanySize, string?>("Company.InvalidSize");

    internal void SetIndustry(int industryId)
    {
        IndustryId = industryId;
    }
    

    public static Company Create(CompanyInput input)
    {
        var company = ValidateAndCreate(input);
        company.SetIndustry(input.IndustryId);
        company.InternalId = input.InternalId;
        company.Id = input.Id;
        EntityFactory.ApplyAudit(company, input.CreatedAt, input.CreatedBy);
        return company;
    }

    private static Company ValidateAndCreate(CompanyInput input)
    {
        var errors = new List<Error>();

        var name = CompanyName.Create(input.Name).Collect<CompanyName, string>(errors);
        var email = CompanyEmail.Create(input.Email).Collect<CompanyEmail, string>(errors);
        var status = CompanyStatus.Create(input.Status).Collect<CompanyStatus, string>(errors);

        var description = CompanyDescription.Create(input.Description)
            .Collect<CompanyDescription, string?>(errors);
        var website = CompanyWebsite.Create(input.Website)
            .Collect<CompanyWebsite, string?>(errors);
        var logo = CompanyLogo.Create(input.Logo)
            .Collect<CompanyLogo, string?>(errors);
        var phone = CompanyPhone.Create(input.Phone)
            .Collect<CompanyPhone, string?>(errors);
        var about = CompanyAbout.Create(input.About)
            .Collect<CompanyAbout, string?>(errors); 
        var externalId = CompanyExternalId.Create(input.ExternalId)
            .Collect<CompanyExternalId, string?>(errors);
        var eeo = CompanyEEO.Create(input.EEO)
            .Collect<CompanyEEO, string?>(errors);
        var founded = CompanyFounded.Create(input.Founded)
            .Collect<CompanyFounded, DateTime?>(errors);
        var size = CompanySize.Create(input.Size)
            .Collect<CompanySize, string?>(errors);

        DomainGuard.AgainstInvalidId(input.IndustryId, "Company.InvalidIndustryId", errors);

        if (errors.Count != 0)
            throw new DomainException("Company.InvalidEntity", errors);

        var company = new Company(name!, email!, status!)
        {
            Description = description,
            Website = website,
            Logo = logo,
            Phone = phone,
            About = about,
            EEO = eeo,
            Founded = founded,
            Size = size,
            ExternalId = externalId
        };

        return company;
    }
}
