using BankingSystem.Account.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Account.Api.Features.Accounts.Commands;

public record CreateAccountCommand(
    string Number,
    string HolderName
) : IRequest<CreateAccountResult>;

public record CreateAccountResult(
    Guid Id,
    string Number,
    string HolderName,
    DateTime OpeningDate,
    decimal Balance
);

public class CreateAccountHandler(AccountDbContext context) : IRequestHandler<CreateAccountCommand, CreateAccountResult>
{
    public async Task<CreateAccountResult> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var existingAccount = await context.Accounts
            .FirstOrDefaultAsync(a => a.Number == request.Number, cancellationToken);

        if (existingAccount != null)
        {
            throw new InvalidOperationException($"Account with number {request.Number} already exists");
        }

        var account = new Models.Account
        {
            Id = Guid.NewGuid(),
            Number = request.Number,
            HolderName = request.HolderName,
            OpeningDate = DateTime.UtcNow,
            Balance = 0,
            IsActive = true
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);

        return new CreateAccountResult(
            account.Id,
            account.Number,
            account.HolderName,
            account.OpeningDate,
            account.Balance
        );
    }
}