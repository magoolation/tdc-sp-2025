using BankingSystem.Account.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Account.Api.Features.Accounts.Queries;

public record GetAccountQuery(string Number) : IRequest<AccountDto>;

public record AccountDto(
    Guid Id,
    string Number,
    string HolderName,
    DateTime OpeningDate,
    decimal Balance,
    bool IsActive
);

public class GetAccountHandler(AccountDbContext context) : IRequestHandler<GetAccountQuery, AccountDto>
{
    public async Task<AccountDto> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Number == request.Number, cancellationToken)
            ?? throw new InvalidOperationException($"Account {request.Number} not found");

        return new AccountDto(
            account.Id,
            account.Number,
            account.HolderName,
            account.OpeningDate,
            account.Balance,
            account.IsActive
        );
    }
}