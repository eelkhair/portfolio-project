using JobBoard.Domain.Entities.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations.Infrastructure;

public class OutboxArchivedMessageConfiguration : IEntityTypeConfiguration<OutboxArchivedMessage>
{
    public void Configure(EntityTypeBuilder<OutboxArchivedMessage> builder)
    {
        builder.ToTable("ArchivedMessages", "outbox");
        builder.ConfigureInfrastructureEntity();
        builder.ConfigureAuditableProperties();
        builder.Property(e => e.EventType).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Payload).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.LastError).HasMaxLength(4000);
        builder.Property(e=> e.TraceParent).HasMaxLength(250);
        
    }
}