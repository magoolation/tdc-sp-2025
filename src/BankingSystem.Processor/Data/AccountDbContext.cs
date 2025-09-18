using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Processor.Data;

public class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Number).IsRequired().HasMaxLength(20);
            entity.HasIndex(a => a.Number).IsUnique();
            entity.Property(a => a.HolderName).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Balance).HasPrecision(18, 2);
        });
    }
}

public class Account
{
    public Guid Id { get; set; }
    public required string Number { get; set; }
    public required string HolderName { get; set; }
    public DateTime OpeningDate { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}