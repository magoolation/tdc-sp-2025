var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL Databases
var accountDb = builder.AddPostgres("postgres-account")
    .AddDatabase("AccountDB");

var transactionDb = builder.AddPostgres("postgres-transaction")
    .AddDatabase("TransactionDB");

// Redis Cache
var cache = builder.AddRedis("cache");

// RabbitMQ Messaging
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Account API - aguarda banco, cache e messaging
var accountApi = builder.AddProject<Projects.BankingSystem_Account_Api>("account-api")
    .WithReference(accountDb)
    .WithReference(cache)
    .WithReference(messaging)
    .WaitFor(accountDb)
    .WaitFor(cache)
    .WaitFor(messaging);

// Transaction API - aguarda banco, cache, messaging e account API
var transactionApi = builder.AddProject<Projects.BankingSystem_Transaction_Api>("transaction-api")
    .WithReference(transactionDb)
    .WithReference(cache)
    .WithReference(messaging)
    .WithReference(accountApi)
    .WaitFor(transactionDb)
    .WaitFor(cache)
    .WaitFor(messaging)
    .WaitFor(accountApi);

// Processor Background Service - aguarda ambos os bancos, messaging e as duas APIs
builder.AddProject<Projects.BankingSystem_Processor>("processor")
    .WithReference(accountDb)
    .WithReference(transactionDb)
    .WithReference(messaging)
    .WaitFor(accountDb)
    .WaitFor(transactionDb)
    .WaitFor(messaging)
    .WaitFor(accountApi)
    .WaitFor(transactionApi);

builder.Build().Run();