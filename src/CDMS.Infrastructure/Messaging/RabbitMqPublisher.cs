using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace CDMS.Infrastructure.Messaging;

public interface IMessagePublisher : IDisposable
{
    Task PublishAsync(string exchange, string routingKey, MessageEnvelope message, CancellationToken cancellationToken = default);
}

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly bool _isConnected;

    public RabbitMqPublisher(
        string hostName,
        int port,
        string userName,
        string password,
        ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                Port = port,
                UserName = userName,
                Password = password,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _isConnected = true;
            _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", hostName, port);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to RabbitMQ at {Host}:{Port}. Running in offline/fallback mode.", hostName, port);
            _isConnected = false;
        }
    }

    public Task PublishAsync(string exchange, string routingKey, MessageEnvelope message, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _channel == null)
        {
            _logger.LogWarning("RabbitMQ not connected. Dropping message {EventId} for routing key {RoutingKey}", message.EventId, routingKey);
            return Task.CompletedTask;
        }

        try
        {
            _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Topic, durable: true);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = message.EventId;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Headers = new Dictionary<string, object>
            {
                { "tenant_id", message.TenantId },
                { "event_type", message.EventType }
            };

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Published event {EventId} to {Exchange}/{RoutingKey}", message.EventId, exchange, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message {EventId} to RabbitMQ", message.EventId);
            throw;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
