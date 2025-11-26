using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Domain.ValueObjects.Industry;

namespace JobBoard.Domain.Entities;

public class Industry : BaseAuditableEntity
{
    // EF Core constructor
    protected Industry()
    {
        Name = string.Empty;
    }

    // Domain constructor
    private Industry(string name)
    {
        Name = name;
    }

    public string Name { get; private set; }

    private readonly List<Company> _companies = [];
    public IReadOnlyCollection<Company> Companies => _companies.AsReadOnly();

    public void SetName(string name) =>
        Name = IndustryName.Create(name)
            .Ensure<IndustryName, string>("Industry.InvalidName")!;

    public void AddCompany(Company company)
    {
        if (company is null)
        {
            throw new DomainException(
                "Industry.NullCompany",
                new[]
                {
                    new Error("Industry.NullCompany", "Company cannot be null.")
                });
        }

        // Duplicate check by NAME
        if (_companies.Any(c =>
                c.Name.Equals(company.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException(
                "Industry.DuplicateCompany",
                new[]
                {
                    new Error(
                        "Industry.DuplicateCompany",
                        $"A company named '{company.Name}' already exists in this Industry.")
                });
        }

        // Assign IndustryId INSIDE the aggregate root only
        company.SetIndustry(Id);

        _companies.Add(company);
    }

    public static Industry Create(string name)
    {
        return ValidateAndCreate(name);
    }

    private static Industry ValidateAndCreate(string name)
    {
        var errors = new List<Error>();

        var validatedName = IndustryName.Create(name)
            .Collect<IndustryName, string>(errors);

        if (errors.Any())
            throw new DomainException("Industry.InvalidEntity", errors);

        return new Industry(validatedName!);
    }
}
