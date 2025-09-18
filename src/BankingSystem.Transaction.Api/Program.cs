using BankingSystem.Shared.Enums;
using BankingSystem.Transaction.Api.Data;
using BankingSystem.Transaction.Api.Features.Transactions.Commands;
using BankingSystem.Transaction.Api.Features.Transactions.Queries;
using BankingSystem.Transaction.Api.Services;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransactionDb")));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(5);
})
.AddStackExchangeRedisOutputCache(options =>
{
    options.Configuration = redisConnection;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();
