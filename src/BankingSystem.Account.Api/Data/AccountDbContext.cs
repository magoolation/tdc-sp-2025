using BankingSystem.Account.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Account.Api.Data;

public class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<Models.Account> Accounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Number).IsRequired().HasMaxLength(20);
            entity.HasIndex(a => a.Number).IsUnique();
            entity.Property(a => a.HolderName).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Balance).HasPrecision(18, 2);
            entity.Property(a => a.CreatedAt).IsRequired();
            entity.Property(a => a.UpdatedAt);
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
            .Where(e => e.Entity is Models.Account && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var account = (Models.Account)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                account.CreatedAt = DateTime.UtcNow;
            }
            
            if (entry.State == EntityState.Modified)
            {
                account.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}