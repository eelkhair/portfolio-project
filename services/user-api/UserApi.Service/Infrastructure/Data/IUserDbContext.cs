using Microsoft.EntityFrameworkCore;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Infrastructure.Data;

public interface IUserDbContext
{
    DbSet<User> Users { get; set; }
}