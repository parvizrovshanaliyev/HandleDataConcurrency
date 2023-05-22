using HandleDataConcurrencies.Models;
using HandleDataConcurrency.Models;
using HandleDataConcurrency.Models.Documents;
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
        public DbSet<Product> Products { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentNumber> DocumentNumbers { get; set; }

        public override int SaveChanges()
        {
            foreach (var entry in ChangeTracker.Entries<CheckConcurrencyEntity>())
            {
                if (entry.State == EntityState.Modified)
                {
                    // Increment the version number of the entity
                    entry.Entity.Version = Guid.NewGuid().ToByteArray();
                }
            }

            return base.SaveChanges();
        }

        // public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        // {
        //     foreach (var entry in ChangeTracker.Entries<CheckConcurrencyEntity>())
        //     {
        //         if (entry.State == EntityState.Modified)
        //         {
        //             // Increment the version number of the entity
        //             entry.Entity.Version=Guid.NewGuid().ToByteArray();
        //         }
        //     }
        //
        //     return base.SaveChangesAsync(cancellationToken);
        // }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
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
                if (entityEntry.State == EntityState.Added)
                {
                    entityEntry.Entity.RowVersion = Guid.NewGuid();
                }
                else
                {
                    // var originalVersion = entityEntry.OriginalValues.GetValue<Guid>("RowVersion");
                    // var currentVersion = entityEntry.Entity.RowVersion;
                    //
                    // if (!originalVersion.Equals(currentVersion))
                    // {
                    //     throw new DbUpdateConcurrencyException("Concurrency conflict occurred. The entity has been modified by another user.");
                    // }

                    entityEntry.Entity.RowVersion = Guid.NewGuid();
                }
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
        
        
        // private ApplyConcurrencyUpdatesResult ApplyConcurrencyUpdates()
        // {
        //     var entities = ChangeTracker.Entries<ICheckConcurrency>()
        //         .Where(e => e.State is EntityState.Modified or EntityState.Added);
        //
        //     foreach (var entityEntry in entities)
        //     {
        //         if (entityEntry.State == EntityState.Added)
        //         {
        //             entityEntry.Entity.Version = Guid.NewGuid().ToByteArray();
        //         }
        //         else
        //         {
        //             var originalVersion = entityEntry.OriginalValues.GetValue<byte[]>("Version");
        //             var currentVersion = entityEntry.Entity.Version;
        //
        //             if (!originalVersion.SequenceEqual(currentVersion))
        //             {
        //                 return ApplyConcurrencyUpdatesResult.Fail(
        //                     "Concurrency conflict occurred. The entity has been modified by another user.");
        //             }
        //
        //             entityEntry.Entity.Version = Guid.NewGuid().ToByteArray();
        //         }
        //     }
        //
        //     return ApplyConcurrencyUpdatesResult.Success();
        // }
        //
        // public class ApplyConcurrencyUpdatesResult
        // {
        //     private bool IsSuccess { get; }
        //     private string? ErrorMessage { get; }
        //
        //     private ApplyConcurrencyUpdatesResult(bool isSuccess, string? errorMessage)
        //     {
        //         IsSuccess = isSuccess;
        //         ErrorMessage = errorMessage;
        //     }
        //
        //     public static ApplyConcurrencyUpdatesResult Success()
        //     {
        //         return new ApplyConcurrencyUpdatesResult(true, null);
        //     }
        //
        //     public static ApplyConcurrencyUpdatesResult Fail(string? errorMessage)
        //     {
        //         return new ApplyConcurrencyUpdatesResult(false, errorMessage);
        //     }
        // }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // modelBuilder.Entity<DocumentNumber>()
            //     .Property(p => p.RowVersion)
            //     .IsRowVersion();
            
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