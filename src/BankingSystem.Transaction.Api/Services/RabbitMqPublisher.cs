using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace BankingSystem.Transaction.Api.Services;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchangeName = "banking.events";

    public RabbitMqPublisher(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:Username"] ?? "admin",
            Password = configuration["RabbitMQ:Password"] ?? "admin"
        };

        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;

        _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Direct, durable: true).Wait();
    }

    public void PublishEvent<T>(T eventData, string routingKey)
    {
        var message = JsonSerializer.Serialize(eventData);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        _channel.BasicPublishAsync(_exchangeName, routingKey, true, properties, body).AsTask().Wait();
    }

    public void Dispose()
    {
        _channel?.CloseAsync().Wait();
        _channel?.Dispose();
        _connection?.CloseAsync().Wait();
        _connection?.Dispose();
    }
}