using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using Database.Models;

namespace Database
{
    public class ParkingDbContext : DbContext
    {
        public ParkingDbContext(DbContextOptions<ParkingDbContext> options) : base(options)
        {
        }

        public DbSet<ParkingLot> ParkingLot { get; set; }
        public DbSet<ParkingStateHistory> ParkingStateHistory { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ParkingLot>().ToTable("ParkingLot");
            modelBuilder.Entity<ParkingStateHistory>().ToTable("ParkingStateHistory");
            modelBuilder.Entity<User>().ToTable("Users");
        }
    }

    public class ParkingDbContextFactory : IDesignTimeDbContextFactory<ParkingDbContext>
    {
        public ParkingDbContext CreateDbContext(string[] args)
        {
            // Build configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../RestApi"))
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ParkingDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            optionsBuilder.UseNpgsql(connectionString);
            
            return new ParkingDbContext(optionsBuilder.Options);
        }
    }
}