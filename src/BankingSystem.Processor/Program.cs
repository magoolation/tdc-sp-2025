using BankingSystem.Processor.Data;
using BankingSystem.Processor.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AccountDb")));

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransactionDb")));

builder.Services.AddHostedService<TransactionProcessorService>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var accountContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    var transactionContext = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();

    await accountContext.Database.EnsureCreatedAsync();
    await transactionContext.Database.EnsureCreatedAsync();
}

await host.RunAsync();
