using AssetTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<User> Users => Set<User>();
}