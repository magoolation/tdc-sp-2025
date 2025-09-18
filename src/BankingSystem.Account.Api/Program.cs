using BankingSystem.Account.Api.Data;
using BankingSystem.Account.Api.Features.Accounts.Commands;
using BankingSystem.Account.Api.Features.Accounts.Queries;
using BankingSystem.Account.Api.Services;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AccountDbContext>("AccountDB");

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddHttpClient<ITransactionApiClient, TransactionApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddServiceDiscovery();

builder.AddRedisOutputCache("cache");

builder.AddRabbitMQClient("messaging");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Ensure database is created and migrations are applied
await app.EnsureDatabaseAsync<AccountDbContext>();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Banking Account API");
    });
}

app.UseOutputCache();

app.MapPost("/api/accounts", async (CreateAccountCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return Results.Created($"/api/accounts/{result.Number}", result);
})
.WithName("CreateAccount")
.WithTags("Accounts");

app.MapPut("/api/accounts/{number}", async (string number, EditAccountCommand command, IMediator mediator) =>
{
    if (number != command.Number)
        return Results.BadRequest("Account number mismatch");

    await mediator.Send(command);
    return Results.NoContent();
})
.WithName("EditAccount")
.WithTags("Accounts");

app.MapDelete("/api/accounts/{number}", async (string number, IMediator mediator) =>
{
    await mediator.Send(new DeactivateAccountCommand(number));
    return Results.NoContent();
})
.WithName("DeactivateAccount")
.WithTags("Accounts");

app.MapGet("/api/accounts/{number}", async (string number, IMediator mediator) =>
{
    var result = await mediator.Send(new GetAccountQuery(number));
    return Results.Ok(result);
})
.WithName("GetAccount")
.WithTags("Accounts")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(1)).Tag("account"));

app.MapGet("/api/accounts/{number}/balance", async (string number, IMediator mediator) =>
{
    var result = await mediator.Send(new GetBalanceQuery(number));
    return Results.Ok(result);
})
.WithName("GetBalance")
.WithTags("Accounts")
.CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)).Tag("balance"));

app.MapGet("/api/accounts/{number}/transactions", async (
    string number,
    DateTime? startDate,
    DateTime? endDate,
    IMediator mediator) =>
{
    var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
    var end = endDate ?? DateTime.UtcNow;

    var result = await mediator.Send(new GetAccountWithTransactionsQuery(number, start, end));
    return Results.Ok(result);
})
.WithName("GetAccountWithTransactions")
.WithTags("Accounts")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(1)).Tag("transactions"));

// Remover a inicialização manual do banco - deixar o Aspire gerenciar
// O Aspire já cuida da criação e migração do banco automaticamente

app.Run();