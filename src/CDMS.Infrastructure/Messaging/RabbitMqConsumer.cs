using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CDMS.Infrastructure.Messaging;

public interface IMessageConsumer : IDisposable
{
    Task SubscribeAsync(
        string queueName,
        string exchange,
        string routingKey,
        Func<MessageEnvelope, ulong, Task<bool>> messageHandler,
        CancellationToken cancellationToken = default);

    void Acknowledge(ulong deliveryTag);
    void Reject(ulong deliveryTag, bool requeue = false);
}

public class RabbitMqConsumer : IMessageConsumer
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly bool _isConnected;
    private string? _consumerTag;

    public RabbitMqConsumer(
        string hostName,
        int port,
        string userName,
        string password,
        ILogger<RabbitMqConsumer> logger)
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
            _logger.LogInformation("Consumer connected to RabbitMQ at {Host}:{Port}", hostName, port);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Consumer failed to connect to RabbitMQ at {Host}:{Port}.", hostName, port);
            _isConnected = false;
        }
    }

    public Task SubscribeAsync(
        string queueName,
        string exchange,
        string routingKey,
        Func<MessageEnvelope, ulong, Task<bool>> messageHandler,
        CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _channel == null)
        {
            _logger.LogWarning("RabbitMQ not connected. Cannot subscribe to queue {QueueName}", queueName);
            return Task.CompletedTask;
        }

        // Declare exchange
        _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Topic, durable: true);

        // Declare DLX (Dead Letter Exchange) and DLQ
        var dlxExchange = $"{exchange}.dlx";
        var dlqName = $"{queueName}.dlq";
        _channel.ExchangeDeclare(exchange: dlxExchange, type: ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: dlqName, exchange: dlxExchange, routingKey: queueName);

        // Declare main queue with DLX arguments
        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", dlxExchange },
            { "x-dead-letter-routing-key", queueName }
        };

        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        _channel.QueueBind(queue: queueName, exchange: exchange, routingKey: routingKey);

        // Fair dispatch
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 20, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var deliveryTag = ea.DeliveryTag;
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var envelope = JsonSerializer.Deserialize<MessageEnvelope>(json);

                if (envelope == null)
                {
                    _logger.LogWarning("Received empty or malformed message envelope, rejecting to DLQ");
                    _channel.BasicReject(deliveryTag, requeue: false);
                    return;
                }

                var success = await messageHandler(envelope, deliveryTag);
                if (success)
                {
                    _channel.BasicAck(deliveryTag, multiple: false);
                }
                else
                {
                    _channel.BasicReject(deliveryTag, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing message {DeliveryTag}", deliveryTag);
                try
                {
                    _channel.BasicReject(deliveryTag, requeue: false);
                }
                catch { }
            }
        };

        _consumerTag = _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Subscribed to queue {QueueName} with tag {ConsumerTag}", queueName, _consumerTag);

        return Task.CompletedTask;
    }

    public void Acknowledge(ulong deliveryTag)
    {
        _channel?.BasicAck(deliveryTag, multiple: false);
    }

    public void Reject(ulong deliveryTag, bool requeue = false)
    {
        _channel?.BasicReject(deliveryTag, requeue: requeue);
    }

    public void Dispose()
    {
        if (!string.IsNullOrEmpty(_consumerTag) && _channel != null && _channel.IsOpen)
        {
            try { _channel.BasicCancel(_consumerTag); } catch { }
        }
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
