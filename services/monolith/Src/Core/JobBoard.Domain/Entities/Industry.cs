using System.Text.Json.Serialization;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Domain.ValueObjects.Industry;

namespace JobBoard.Domain.Entities;

public class Industry : BaseAuditableEntity
{
    protected Industry()
    {
        Name = string.Empty;
    }
    
    private Industry(string name)
    {
        Name = name;
    }

    public string Name { get; private set; }

    private readonly List<Company> _companies = [];
    [JsonIgnore]
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
                [
                    new Error("Industry.NullCompany", "Company cannot be null.")
                ]);
        }

        if (_companies.Any(c =>
                c.Name.Equals(company.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException(
                "Industry.DuplicateCompany",
                [
                    new Error(
                        "Industry.DuplicateCompany",
                        $"A company named '{company.Name}' already exists in this Industry.")
                ]);
        }

        company.SetIndustry(0);

        _companies.Add(company);
    }
    public void RemoveCompany(Company company)
    {
        if (company is null) return;

        if (_companies.Remove(company))
            company.SetIndustry(0);
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
