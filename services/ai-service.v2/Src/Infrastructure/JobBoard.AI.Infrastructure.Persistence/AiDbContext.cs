using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.Drafts;
using Microsoft.EntityFrameworkCore;


namespace JobBoard.AI.Infrastructure.Persistence;

public sealed class AiDbContext : DbContext, IAiDbContext
{
    public DbSet<JobEmbedding> JobEmbeddings => Set<JobEmbedding>();
    public DbSet<ResumeEmbedding> ResumeEmbeddings => Set<ResumeEmbedding>();
    public DbSet<MatchExplanation> MatchExplanations => Set<MatchExplanation>();
    

    public AiDbContext(DbContextOptions<AiDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.Entity<JobCandidate>(b =>
        {
            b.HasNoKey();
            b.ToView(null);
            b.Ignore(x => x.MatchSummary);
            b.Ignore(x => x.MatchDetails);
            b.Ignore(x => x.MatchGaps);
        });
        modelBuilder.Entity<JobEmbedding>(b =>
        {
            b.ToTable("job_embeddings");

            b.Property(x => x.VectorData)
                .HasColumnType("vector(1536)")
                .IsRequired();

            b.Ignore(x => x.Vector);

            b.Property(x => x.Provider)
                .HasConversion(v => v.Value, v => new(v));

            b.Property(x => x.Model)
                .HasConversion(v => v.Value, v => new(v));

        });

        modelBuilder.Entity<ResumeEmbedding>(b =>
        {
            b.ToTable("resume_embeddings");

            b.Property(x => x.VectorData)
                .HasColumnType("vector(1536)")
                .IsRequired();
            b.Ignore(x => x.Vector);

            b.Property(x => x.SkillsVectorData)
                .HasColumnName("skills_vector")
                .HasColumnType("vector(1536)");
            b.Ignore(x => x.SkillsVector);

            b.Property(x => x.ExperienceVectorData)
                .HasColumnName("experience_vector")
                .HasColumnType("vector(1536)");
            b.Ignore(x => x.ExperienceVector);

            b.Property(x => x.Provider)
                .HasConversion(v => v.Value, v => new(v));

            b.Property(x => x.Model)
                .HasConversion(v => v.Value, v => new(v));
        });

        modelBuilder.Entity<MatchExplanation>(b =>
        {
            b.ToTable("match_explanations");
            b.HasIndex(x => new { x.ResumeUId, x.JobId }).IsUnique();
            b.HasIndex(x => x.ResumeUId);
            b.HasIndex(x => x.JobId);
        });

    }
}