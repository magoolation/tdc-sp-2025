using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace BankingSystem.Transaction.Api.Services;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName = "banking.events";

    public RabbitMqPublisher(IConnection connection)
    {
        _connection = connection;
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: true);
    }

    public void PublishEvent<T>(T eventData, string routingKey)
    {
        var message = JsonSerializer.Serialize(eventData);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(_exchangeName, routingKey, properties, body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
    }
}