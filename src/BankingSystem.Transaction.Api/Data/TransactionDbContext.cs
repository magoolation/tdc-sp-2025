using BankingSystem.Transaction.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Transaction.Api.Data;

public class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
{
    public DbSet<Models.Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.AccountNumber).IsRequired().HasMaxLength(20);
            entity.Property(t => t.Description).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.Type).IsRequired();
            entity.Property(t => t.Status).IsRequired();
            entity.Property(t => t.CreatedAt).IsRequired();
            entity.Property(t => t.UpdatedAt);
            entity.HasIndex(t => t.AccountNumber);
            entity.HasIndex(t => new { t.AccountNumber, t.Date });
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Models.Transaction && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var transaction = (Models.Transaction)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                transaction.CreatedAt = DateTime.UtcNow;
            }
            
            if (entry.State == EntityState.Modified)
            {
                transaction.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}