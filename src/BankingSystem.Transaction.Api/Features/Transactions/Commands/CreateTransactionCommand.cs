using BankingSystem.Shared.Enums;
using BankingSystem.Shared.Events;
using BankingSystem.Transaction.Api.Data;
using BankingSystem.Transaction.Api.Services;
using MediatR;

namespace BankingSystem.Transaction.Api.Features.Transactions.Commands;

public record CreateTransactionCommand(
    string AccountNumber,
    TransactionType Type,
    string Description,
    decimal Amount
) : IRequest<CreateTransactionResult>;

public record CreateTransactionResult(Guid TransactionId);

public class CreateTransactionHandler(
    TransactionDbContext context,
    IRabbitMqPublisher publisher
) : IRequestHandler<CreateTransactionCommand, CreateTransactionResult>
{
    public async Task<CreateTransactionResult> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = new Models.Transaction
        {
            Id = Guid.NewGuid(),
            AccountNumber = request.AccountNumber,
            Type = request.Type,
            Description = request.Description,
            Amount = request.Amount,
            Date = DateTime.UtcNow,
            Status = TransactionStatus.Completed
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);

        var eventData = new TransactionCreatedEvent(
            transaction.Id,
            transaction.AccountNumber,
            transaction.Type,
            transaction.Description,
            transaction.Date,
            transaction.Amount
        );

        publisher.PublishEvent(eventData, "transaction.created");

        return new CreateTransactionResult(transaction.Id);
    }
}