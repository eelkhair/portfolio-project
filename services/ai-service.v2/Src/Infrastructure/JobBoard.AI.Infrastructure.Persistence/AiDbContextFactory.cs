using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobBoard.AI.Infrastructure.Persistence;

public sealed class AiDbContextFactory
    : IDesignTimeDbContextFactory<AiDbContext>
{
    public AiDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AiDbContext>()
            .UseNpgsql("Host=192.168.1.160;Port=5432;Database=ai_service;Username=ai_user;Password=Pass321$;Pooling=true;Maximum Pool Size=20;", o=>o.UseVector())
            .Options;

        return new AiDbContext(options);
    }
}