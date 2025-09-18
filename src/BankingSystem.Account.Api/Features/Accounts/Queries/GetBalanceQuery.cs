using BankingSystem.Account.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Account.Api.Features.Accounts.Queries;

public record GetBalanceQuery(string Number) : IRequest<BalanceDto>;

public record BalanceDto(string AccountNumber, decimal Balance);

public class GetBalanceHandler(AccountDbContext context) : IRequestHandler<GetBalanceQuery, BalanceDto>
{
    public async Task<BalanceDto> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        var account = await context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Number == request.Number, cancellationToken)
            ?? throw new InvalidOperationException($"Account {request.Number} not found");

        return new BalanceDto(account.Number, account.Balance);
    }
}