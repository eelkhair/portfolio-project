using Elkhair.Dev.Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Infrastructure.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ConfigureBaseAuditableEntity();
        builder.ToTable("Companies");
        builder.Property(c => c.Name).HasMaxLength(50);
        builder.Property(c => c.Auth0OrganizationId).HasMaxLength(50);
        builder.HasMany(c => c.UserCompanies).WithOne(uc => uc.Company).HasForeignKey(uc => uc.CompanyId);
        builder.HasIndex(c => c.Auth0OrganizationId).IsUnique(true);
    }
}