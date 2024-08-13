using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace Contoso.Infrastructure.Messaging
{
    public class EventPublisher(IProducer<string, string> producer, ILogger<EventPublisher> logger) : IEventPublisher
    {
        public async Task PublishAsync<T>(string topicName, T evento) where T : class
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? "traceid não disponível";
            var message = new Message<string, string>
            {
                Key = traceId,
                Value = JsonConvert.SerializeObject(evento, JsonSerializationSettings.Settings),
                Headers = new Headers
                {
                    { "traceid", Encoding.UTF8.GetBytes(traceId) },
                },
            };

            try
            {
                var deliveryResult = await producer.ProduceAsync(topicName, message).ConfigureAwait(false);
                logger.LogInformation("Delivered '{payload}' to '{TopicPartitionOffset}'", deliveryResult.Value, deliveryResult.TopicPartitionOffset);
            }
            catch (ProduceException<string, string> e)
            {
                using var scope = logger.BeginScope(new { traceId });
                logger.LogError(e, "Delivery failed: {Reason}", e.Error.Reason);
            }
        }
    }
}