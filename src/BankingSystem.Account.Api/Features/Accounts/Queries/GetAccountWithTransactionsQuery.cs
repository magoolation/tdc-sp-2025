using BankingSystem.Account.Api.Services;
using BankingSystem.Shared.DTOs;
using MediatR;

namespace BankingSystem.Account.Api.Features.Accounts.Queries;

public record GetAccountWithTransactionsQuery(
    string Number,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<AccountWithTransactionsDto>;

public record AccountWithTransactionsDto(
    Guid Id,
    string Number,
    string HolderName,
    DateTime OpeningDate,
    decimal Balance,
    bool IsActive,
    List<TransactionDto> Transactions
);

public class GetAccountWithTransactionsHandler(
    IMediator mediator,
    ITransactionApiClient transactionApiClient
) : IRequestHandler<GetAccountWithTransactionsQuery, AccountWithTransactionsDto>
{
    public async Task<AccountWithTransactionsDto> Handle(
        GetAccountWithTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var account = await mediator.Send(new GetAccountQuery(request.Number), cancellationToken);

        var transactions = await transactionApiClient.GetTransactionsByPeriodAsync(
            request.Number,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return new AccountWithTransactionsDto(
            account.Id,
            account.Number,
            account.HolderName,
            account.OpeningDate,
            account.Balance,
            account.IsActive,
            transactions
        );
    }
}