using Contoso.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Contoso
{
    public static class Extensions
    {
        /// <summary>
        /// Nesse exemplo utilizamos o Kafka como serviço de mensageria, mas poderia ser qualquer outro como RabbitMQ, Azure Service Bus, etc.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddContosoKafkaProducer(this IHostApplicationBuilder builder)
        {

            builder.AddKafkaProducer<string, string>("kafka");
            builder.Services.AddSingleton<IEventPublisher, EventPublisher>();
            return builder.Services;
        }
    }
}