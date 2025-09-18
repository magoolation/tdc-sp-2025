using BankingSystem.Shared.Enums;

namespace BankingSystem.Shared.Events;

public record TransactionCreatedEvent(
    Guid TransactionId,
    string AccountNumber,
    TransactionType Type,
    string Description,
    DateTime Date,
    decimal Amount
);