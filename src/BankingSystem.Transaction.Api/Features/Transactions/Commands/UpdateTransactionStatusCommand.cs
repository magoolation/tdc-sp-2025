using BankingSystem.Shared.Enums;
using BankingSystem.Transaction.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Transaction.Api.Features.Transactions.Commands;

public record UpdateTransactionStatusCommand(
    Guid TransactionId,
    TransactionStatus Status
) : IRequest<Unit>;

public class UpdateTransactionStatusHandler(
    TransactionDbContext context
) : IRequestHandler<UpdateTransactionStatusCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateTransactionStatusCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken)
            ?? throw new InvalidOperationException($"Transaction {request.TransactionId} not found");

        transaction.Status = request.Status;
        transaction.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}