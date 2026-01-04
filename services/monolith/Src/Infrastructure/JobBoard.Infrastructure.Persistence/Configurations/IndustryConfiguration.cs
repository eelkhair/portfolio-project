using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations;

public class IndustryConfiguration : IEntityTypeConfiguration<Industry>
{
    public void Configure(EntityTypeBuilder<Industry> builder)
    {
        builder.ToTable("Industries","Company");

        builder.ConfigureBusinessEntity();
        builder.ConfigureAuditableProperties();
        
        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(i => i.Name)
            .IsUnique();
        
        builder.Metadata
            .FindNavigation(nameof(Industry.Companies))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(i => i.Companies)
            .WithOne(c => c.Industry)
            .HasForeignKey(c => c.IndustryId)
            .OnDelete(DeleteBehavior.Restrict); // avoid cascade deletes
    }
}