using BankingSystem.Shared.DTOs;

namespace BankingSystem.Account.Api.Services;

public interface ITransactionApiClient
{
    Task<List<TransactionDto>> GetTransactionsByPeriodAsync(
        string accountNumber,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}