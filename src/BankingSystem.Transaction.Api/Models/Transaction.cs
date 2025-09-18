using BankingSystem.Shared.Enums;

namespace BankingSystem.Transaction.Api.Models;

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