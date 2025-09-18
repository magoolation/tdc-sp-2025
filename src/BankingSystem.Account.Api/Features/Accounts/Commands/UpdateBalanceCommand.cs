using BankingSystem.Account.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Account.Api.Features.Accounts.Commands;

public record UpdateBalanceCommand(
    string AccountNumber,
    decimal Amount,
    bool IsCredit
) : IRequest<Unit>;

public class UpdateBalanceHandler(AccountDbContext context) : IRequestHandler<UpdateBalanceCommand, Unit>
{
    public async Task<Unit> Handle(UpdateBalanceCommand request, CancellationToken cancellationToken)
    {
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Number == request.AccountNumber, cancellationToken)
            ?? throw new InvalidOperationException($"Account {request.AccountNumber} not found");

        if (request.IsCredit)
        {
            account.Balance += request.Amount;
        }
        else
        {
            if (account.Balance < request.Amount)
            {
                throw new InvalidOperationException("Insufficient balance");
            }
            account.Balance -= request.Amount;
        }

        account.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}