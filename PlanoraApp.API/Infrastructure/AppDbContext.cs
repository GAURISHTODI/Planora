using Microsoft.EntityFrameworkCore;
using PlanoraApp.API.Services.UserService.Models;
using PlanoraApp.API.Services.VaultService;

namespace PlanoraApp.API.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<VaultItem> VaultItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configuration for schemas will go here
        }
    }
}
