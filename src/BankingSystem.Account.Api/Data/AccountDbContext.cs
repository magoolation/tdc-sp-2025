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
        });
    }
}