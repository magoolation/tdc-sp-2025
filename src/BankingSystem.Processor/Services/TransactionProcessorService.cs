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
    private readonly IConnection _connection;
    private IModel? _channel;
    private readonly string _exchangeName = "banking.events";
    private readonly string _queueName = "transaction.processor";

    public TransactionProcessorService(
        ILogger<TransactionProcessorService> logger,
        IServiceProvider serviceProvider,
        IConnection connection)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _connection = connection;
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
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: true);

        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueBind(_queueName, _exchangeName, "transaction.created");
        _channel.QueueBind(_queueName, _exchangeName, "transaction.cancelled");

        _channel.BasicQos(0, 1, false);
        await Task.CompletedTask;
    }

    private async Task ProcessMessages(CancellationToken stoppingToken)
    {
        if (_channel == null) return;

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
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

                _channel!.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                _channel!.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel!.BasicConsume(
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
            _channel.Close();
            _channel.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}