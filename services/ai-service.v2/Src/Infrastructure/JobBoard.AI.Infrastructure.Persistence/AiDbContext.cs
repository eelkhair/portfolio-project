using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.Drafts;
using Microsoft.EntityFrameworkCore;


namespace JobBoard.AI.Infrastructure.Persistence;

public sealed class AiDbContext : DbContext, IAiDbContext
{
    public DbSet<Draft> Drafts => Set<Draft>();
    public DbSet<DraftEmbedding> DraftEmbeddings => Set<DraftEmbedding>();
    

    public AiDbContext(DbContextOptions<AiDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Draft>(b =>
        {
            b.ToTable("drafts");
            b.HasKey(x => x.Id);

            b.Property(x => x.Type)
                .HasConversion(v => v.Value, v => new DraftType(v));

            b.Property(x => x.Status)
                .HasConversion(v => v.Value, v => new DraftStatus(v));

            b.Property(x => x.ContentJson)
                .HasColumnType("jsonb");

            b.HasMany(x => x.Embeddings)
                .WithOne(x => x.Draft)
                .HasForeignKey(x => x.DraftId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DraftEmbedding>(b =>
        {
            b.ToTable("draft_embeddings");

            b.Property(x => x.VectorData)
                .HasColumnType("vector(1536)")
                .IsRequired();

            b.Ignore(x => x.Vector);

            b.Property(x => x.Provider)
                .HasConversion(v => v.Value, v => new(v));

            b.Property(x => x.Model)
                .HasConversion(v => v.Value, v => new(v));
            
            b.HasOne(x => x.Draft)
                .WithMany(x => x.Embeddings)
                .HasForeignKey(x => x.DraftId)
                .OnDelete(DeleteBehavior.Cascade);
        });


    }
}