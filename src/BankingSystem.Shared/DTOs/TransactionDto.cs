using BankingSystem.Shared.Enums;

namespace BankingSystem.Shared.DTOs;

public record TransactionDto(
    Guid Id,
    string AccountNumber,
    TransactionType Type,
    string Description,
    DateTime Date,
    decimal Amount,
    TransactionStatus Status
);