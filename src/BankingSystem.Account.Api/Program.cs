using BankingSystem.Account.Api.Data;
using BankingSystem.Account.Api.Features.Accounts.Commands;
using BankingSystem.Account.Api.Features.Accounts.Queries;
using BankingSystem.Account.Api.Services;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AccountDb")));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddHttpClient<ITransactionApiClient, TransactionApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:TransactionApi"] ?? "http://localhost:5001/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();
