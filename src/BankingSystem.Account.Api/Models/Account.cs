namespace BankingSystem.Account.Api.Models;

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