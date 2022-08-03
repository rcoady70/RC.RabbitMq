using RC.RabbitMq.Consumers;
using RC.RabbitMq.Message;

namespace RC.MessageQueue
{
    public interface IRabbitMQueueManager
    {
        void AddMessageToQueue<TMessage>(TMessage message) where TMessage : IntegrationMessage;
        void Dispose();
        object Get_consumers<TMessage, TConsumer>()
            where TMessage : IntegrationMessage
            where TConsumer : IConsumeMessage;
        void RegisterConsumer<TMessage, TConsumer>()
            where TMessage : IntegrationMessage
            where TConsumer : IConsumeMessage;
        void SendAck(ulong deliveryTag);
        void SendNack(ulong deliveryTag, bool requeue);
    }
}