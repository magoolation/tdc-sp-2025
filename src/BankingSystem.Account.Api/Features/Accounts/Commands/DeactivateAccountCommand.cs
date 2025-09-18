using BankingSystem.Account.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Account.Api.Features.Accounts.Commands;

public record DeactivateAccountCommand(string Number) : IRequest<Unit>;

public class DeactivateAccountHandler(AccountDbContext context) : IRequestHandler<DeactivateAccountCommand, Unit>
{
    public async Task<Unit> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Number == request.Number, cancellationToken)
            ?? throw new InvalidOperationException($"Account {request.Number} not found");

        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}