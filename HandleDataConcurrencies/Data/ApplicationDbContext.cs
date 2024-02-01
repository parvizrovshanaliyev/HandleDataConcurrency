using HandleDataConcurrency.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace HandleDataConcurrency.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentNumber> DocumentNumbers { get; set; }
        
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyConcurrencyUpdates();
            ApplyAuditUpdates();

            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyConcurrencyUpdates()
        {
            var entities = ChangeTracker.Entries<ICheckConcurrency>()
                .Where(e => e.State is EntityState.Modified or EntityState.Added);

            foreach (var entityEntry in entities)
            {
                entityEntry.Entity.RowVersion = Guid.NewGuid();
            }
        }
        
        private void ApplyAuditUpdates()
        {
            var entities = ChangeTracker.Entries<IAudit>()
                .Where(e => e.State is EntityState.Modified or EntityState.Added);

            foreach (var entityEntry in entities)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    entityEntry.Entity.CreatedDate = DateTime.Now;
                }
                else
                {
                    entityEntry.Entity.UpdatedDate = DateTime.Now;

                }
            }
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<Document>()
                .Property(x=>x.Status)
                .HasMaxLength(50)
                .HasConversion(x => x.ToString(),
                    x => (DocumentStatus)Enum.Parse(typeof(DocumentStatus), x));
            
            modelBuilder.Entity<Document>()
                .Property(x=>x.DocumentType)
                .HasMaxLength(50)
                .HasConversion(x => x.ToString(),
                    x => (DocumentType)Enum.Parse(typeof(DocumentType), x));
            
            modelBuilder.Entity<DocumentNumber>()
                .Property(x=>x.DocumentType)
                .HasMaxLength(50)
                .HasConversion(x => x.ToString(),
                    x => (DocumentType)Enum.Parse(typeof(DocumentType), x));
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