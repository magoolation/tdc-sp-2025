using BankingSystem.Processor.Data;
using BankingSystem.Processor.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AccountDbContext>("AccountDB");
builder.AddNpgsqlDbContext<TransactionDbContext>("TransactionDB");

builder.AddRabbitMQClient("messaging");

builder.Services.AddHostedService<TransactionProcessorService>();

var host = builder.Build();

// Remover a inicialização manual do banco - deixar o Aspire gerenciar
// O Aspire já cuida da criação e migração do banco automaticamente

await host.RunAsync();