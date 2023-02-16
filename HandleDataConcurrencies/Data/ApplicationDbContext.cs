using HandleDataConcurrencies.Models;
using Microsoft.EntityFrameworkCore;

namespace HandleDataConcurrencies.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .Property(p => p.RowVersion)
                .IsRowVersion();
        }
    }
}
