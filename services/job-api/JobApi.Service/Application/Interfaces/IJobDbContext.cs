using System.Security.Claims;
using JobApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace JobApi.Application.Interfaces;

public interface IJobDbContext
{
    DbSet<Company> Companies { get; set; }
    DbSet<Job> Jobs { get; set; }
    DbSet<Qualification> Qualifications { get; set; }
    DbSet<Responsibility> Responsibilities { get; set; }
    ChangeTracker ChangeTracker { get;}
    Task<int> SaveChangesAsync(ClaimsPrincipal user, CancellationToken cancellationToken);
}