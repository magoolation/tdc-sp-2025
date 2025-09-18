using BankingSystem.Account.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Account.Api.Features.Accounts.Commands;

public record EditAccountCommand(
    string Number,
    string HolderName
) : IRequest<Unit>;

public class EditAccountHandler(AccountDbContext context) : IRequestHandler<EditAccountCommand, Unit>
{
    public async Task<Unit> Handle(EditAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Number == request.Number, cancellationToken)
            ?? throw new InvalidOperationException($"Account {request.Number} not found");

        account.HolderName = request.HolderName;
        account.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}