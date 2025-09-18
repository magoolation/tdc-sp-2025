using System.Text;
using System.Text.Json;
using BankingSystem.Processor.Data;
using BankingSystem.Shared.Enums;
using BankingSystem.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BankingSystem.Processor.Services;

public class TransactionProcessorService : BackgroundService
{
    private readonly ILogger<TransactionProcessorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _exchangeName = "banking.events";
    private readonly string _queueName = "transaction.processor";

    public TransactionProcessorService(
        ILogger<TransactionProcessorService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await InitializeRabbitMq();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing messages");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task InitializeRabbitMq()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "admin",
            Password = _configuration["RabbitMQ:Password"] ?? "admin"
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Direct, durable: true);

        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await _channel.QueueBindAsync(_queueName, _exchangeName, "transaction.created");
        await _channel.QueueBindAsync(_queueName, _exchangeName, "transaction.cancelled");

        await _channel.BasicQosAsync(0, 1, false);
    }

    private async Task ProcessMessages(CancellationToken stoppingToken)
    {
        if (_channel == null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                _logger.LogInformation($"Processing message with routing key: {routingKey}");

                using var scope = _serviceProvider.CreateScope();

                if (routingKey == "transaction.created")
                {
                    await ProcessTransactionCreated(message, scope.ServiceProvider);
                }
                else if (routingKey == "transaction.cancelled")
                {
                    await ProcessTransactionCancelled(message, scope.ServiceProvider);
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessTransactionCreated(string message, IServiceProvider serviceProvider)
    {
        var eventData = JsonSerializer.Deserialize<TransactionCreatedEvent>(message);
        if (eventData == null) return;

        var accountDb = serviceProvider.GetRequiredService<AccountDbContext>();
        var transactionDb = serviceProvider.GetRequiredService<TransactionDbContext>();

        var account = await accountDb.Accounts
            .FirstOrDefaultAsync(a => a.Number == eventData.AccountNumber);

        if (account == null)
        {
            _logger.LogWarning($"Account {eventData.AccountNumber} not found");

            var transaction = await transactionDb.Transactions
                .FirstOrDefaultAsync(t => t.Id == eventData.TransactionId);

            if (transaction != null)
            {
                transaction.Status = TransactionStatus.Rejected;
                transaction.UpdatedAt = DateTime.UtcNow;
                await transactionDb.SaveChangesAsync();
            }
            return;
        }

        bool isCredit = eventData.Type == TransactionType.Deposit ||
                       eventData.Type == TransactionType.Salary ||
                       eventData.Type == TransactionType.Reversal;

        if (isCredit)
        {
            account.Balance += eventData.Amount;
        }
        else
        {
            if (account.Balance < eventData.Amount)
            {
                _logger.LogWarning($"Insufficient balance for account {eventData.AccountNumber}");

                var transaction = await transactionDb.Transactions
                    .FirstOrDefaultAsync(t => t.Id == eventData.TransactionId);

                if (transaction != null)
                {
                    transaction.Status = TransactionStatus.Rejected;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await transactionDb.SaveChangesAsync();
                }
                return;
            }
            account.Balance -= eventData.Amount;
        }

        account.UpdatedAt = DateTime.UtcNow;
        await accountDb.SaveChangesAsync();

        _logger.LogInformation($"Transaction {eventData.TransactionId} processed successfully");
    }

    private async Task ProcessTransactionCancelled(string message, IServiceProvider serviceProvider)
    {
        var eventData = JsonSerializer.Deserialize<TransactionCancelledEvent>(message);
        if (eventData == null) return;

        var accountDb = serviceProvider.GetRequiredService<AccountDbContext>();
        var transactionDb = serviceProvider.GetRequiredService<TransactionDbContext>();

        var account = await accountDb.Accounts
            .FirstOrDefaultAsync(a => a.Number == eventData.AccountNumber);

        if (account == null)
        {
            _logger.LogWarning($"Account {eventData.AccountNumber} not found");
            return;
        }

        account.Balance += eventData.Amount;
        account.UpdatedAt = DateTime.UtcNow;
        await accountDb.SaveChangesAsync();

        var reversalTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountNumber = eventData.AccountNumber,
            Type = TransactionType.Reversal,
            Description = $"Estorno da transação {eventData.TransactionId}",
            Amount = eventData.Amount,
            Date = DateTime.UtcNow,
            Status = TransactionStatus.Completed
        };

        transactionDb.Transactions.Add(reversalTransaction);
        await transactionDb.SaveChangesAsync();

        _logger.LogInformation($"Transaction {eventData.TransactionId} cancelled and reversed");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Transaction Processor Service is stopping");

        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}