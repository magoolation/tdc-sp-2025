using BankingSystem.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Processor.Data;

public class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>(entity =>
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

public class Transaction
{
    public Guid Id { get; set; }
    public required string AccountNumber { get; set; }
    public TransactionType Type { get; set; }
    public required string Description { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}