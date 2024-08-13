namespace Contoso.Infrastructure.Messaging
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string topicName,T evento) where T : class;
    }
}