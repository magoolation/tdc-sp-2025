using BankingSystem.Transaction.Api.Data;
using BankingSystem.Transaction.Api.Features.Transactions.Commands;
using BankingSystem.Transaction.Api.Features.Transactions.Queries;
using BankingSystem.Transaction.Api.Services;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<TransactionDbContext>("TransactionDB");

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.AddRabbitMQClient("messaging");

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.AddRedisOutputCache("cache");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Ensure database is created and migrations are applied
await app.EnsureDatabaseAsync<TransactionDbContext>();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Banking Transaction API");
    });
}

app.UseOutputCache();

app.MapPost("/api/transactions", async (CreateTransactionCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return Results.Accepted($"/api/transactions/{result.TransactionId}", result);
})
.WithName("CreateTransaction")
.WithTags("Transactions");

app.MapDelete("/api/transactions/{id}", async (Guid id, IMediator mediator) =>
{
    await mediator.Send(new CancelTransactionCommand(id));
    return Results.Accepted();
})
.WithName("CancelTransaction")
.WithTags("Transactions");

app.MapGet("/api/transactions/account/{accountNumber}", async (
    string accountNumber,
    DateTime? startDate,
    DateTime? endDate,
    IMediator mediator) =>
{
    var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
    var end = endDate ?? DateTime.UtcNow;

    var result = await mediator.Send(new GetTransactionsByPeriodQuery(accountNumber, start, end));
    return Results.Ok(result);
})
.WithName("GetTransactionsByPeriod")
.WithTags("Transactions")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(1)).Tag("transactions"));

app.MapPut("/api/transactions/{id}/status", async (Guid id, UpdateTransactionStatusCommand command, IMediator mediator) =>
{
    if (id != command.TransactionId)
        return Results.BadRequest("Transaction ID mismatch");

    await mediator.Send(command);
    return Results.NoContent();
})
.WithName("UpdateTransactionStatus")
.WithTags("Transactions");

// Remover a inicialização manual do banco - deixar o Aspire gerenciar
// O Aspire já cuida da criação e migração do banco automaticamente

app.Run();