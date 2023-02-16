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
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            base.OnConfiguring(optionsBuilder.UseLoggerFactory(ApplicationLoggerFactory));
#endif
        }
        
        /* It allows us to see the generated sql script while it is in debug mode in linq queries. */
        private static readonly ILoggerFactory ApplicationLoggerFactory
            = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter((category, level) =>
                        category == DbLoggerCategory.Database.Command.Name
                        && level == LogLevel.Information)
                    .AddDebug();
            });
    }
}
