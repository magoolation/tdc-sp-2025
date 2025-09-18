namespace BankingSystem.Shared.Events;

public record TransactionCancelledEvent(
    Guid TransactionId,
    string AccountNumber,
    decimal Amount
);