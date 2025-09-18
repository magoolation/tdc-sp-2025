using BankingSystem.Shared.Enums;
using BankingSystem.Shared.Events;
using BankingSystem.Transaction.Api.Data;
using BankingSystem.Transaction.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Transaction.Api.Features.Transactions.Commands;

public record CancelTransactionCommand(Guid TransactionId) : IRequest<Unit>;

public class CancelTransactionHandler(
    TransactionDbContext context,
    IRabbitMqPublisher publisher
) : IRequestHandler<CancelTransactionCommand, Unit>
{
    public async Task<Unit> Handle(
        CancelTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken)
            ?? throw new InvalidOperationException($"Transaction {request.TransactionId} not found");

        if (transaction.Status == TransactionStatus.Cancelled)
        {
            throw new InvalidOperationException($"Transaction {request.TransactionId} is already cancelled");
        }

        transaction.Status = TransactionStatus.Cancelled;
        transaction.UpdatedAt = DateTime.UtcNow;

        var reversalTransaction = new Models.Transaction
        {
            Id = Guid.NewGuid(),
            AccountNumber = transaction.AccountNumber,
            Type = TransactionType.Reversal,
            Description = $"Estorno da transação {transaction.Id}",
            Amount = transaction.Amount,
            Date = DateTime.UtcNow,
            Status = TransactionStatus.Completed
        };

        context.Transactions.Add(reversalTransaction);
        await context.SaveChangesAsync(cancellationToken);

        var eventData = new TransactionCancelledEvent(
            transaction.Id,
            transaction.AccountNumber,
            transaction.Amount
        );

        publisher.PublishEvent(eventData, "transaction.cancelled");

        return Unit.Value;
    }
}