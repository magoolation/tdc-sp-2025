namespace BankingSystem.Transaction.Api.Services;

public interface IRabbitMqPublisher
{
    void PublishEvent<T>(T eventData, string routingKey);
}