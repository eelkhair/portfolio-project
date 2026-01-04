using Elkhair.Dev.Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Infrastructure.Data.Configurations;

public class UserCompanyConfiguration : IEntityTypeConfiguration<UserCompany>
{
    public void Configure(EntityTypeBuilder<UserCompany> builder)
    {
        builder.ToTable("UserCompanies");
        builder.ConfigureBaseAuditableEntity();
        builder.HasIndex(uc => new {uc.UserId, uc.CompanyId}).IsUnique();
        builder.HasOne(uc => uc.User).WithMany(u => u.UserCompanies).HasForeignKey(uc => uc.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(uc => uc.Company).WithMany(c => c.UserCompanies).HasForeignKey(uc => uc.CompanyId).OnDelete(DeleteBehavior.Cascade);
        
    }
}