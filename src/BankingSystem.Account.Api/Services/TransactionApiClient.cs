using System.Text.Json;
using BankingSystem.Shared.DTOs;

namespace BankingSystem.Account.Api.Services;

public class TransactionApiClient(HttpClient httpClient) : ITransactionApiClient
{
    public async Task<List<TransactionDto>> GetTransactionsByPeriodAsync(
        string accountNumber,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"api/transactions/account/{accountNumber}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<List<TransactionDto>>(content, options) ?? [];
    }
}