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
            entity.HasIndex(t => t.AccountNumber);
            entity.HasIndex(t => new { t.AccountNumber, t.Date });
        });
    }
}