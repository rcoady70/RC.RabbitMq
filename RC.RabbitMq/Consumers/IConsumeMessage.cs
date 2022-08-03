using RC.RabbitMq.Message;

namespace RC.RabbitMq.Consumers
{
    public interface IConsumeMessage<T> where T : IntegrationMessage<T>
    {
        void ProcessMessage(T message);
    }
}