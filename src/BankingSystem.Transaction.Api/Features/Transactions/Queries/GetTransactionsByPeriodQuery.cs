using BankingSystem.Shared.DTOs;
using BankingSystem.Transaction.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Transaction.Api.Features.Transactions.Queries;

public record GetTransactionsByPeriodQuery(
    string AccountNumber,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<List<TransactionDto>>;

public class GetTransactionsByPeriodHandler(
    TransactionDbContext context
) : IRequestHandler<GetTransactionsByPeriodQuery, List<TransactionDto>>
{
    public async Task<List<TransactionDto>> Handle(
        GetTransactionsByPeriodQuery request,
        CancellationToken cancellationToken)
    {
        var transactions = await context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountNumber == request.AccountNumber
                && t.Date >= request.StartDate
                && t.Date <= request.EndDate)
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionDto(
                t.Id,
                t.AccountNumber,
                t.Type,
                t.Description,
                t.Date,
                t.Amount,
                t.Status
            ))
            .ToListAsync(cancellationToken);

        return transactions;
    }
}