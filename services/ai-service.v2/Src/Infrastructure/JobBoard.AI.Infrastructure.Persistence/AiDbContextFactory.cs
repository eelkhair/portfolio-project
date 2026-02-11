using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobBoard.AI.Infrastructure.Persistence;

public sealed class AiDbContextFactory
    : IDesignTimeDbContextFactory<AiDbContext>
{
    public AiDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AiDbContext>()
            .UseNpgsql("", o=>o.UseVector())
            .Options;

        return new AiDbContext(options);
    }
}