using RC.RabbitMq.Consumers;
using RC.RabbitMq.Message;

namespace RC.MessageQueue
{
    public interface IRabbitMQueueManager
    {
        void AddMessageToQueue<TMessage>(TMessage message) where TMessage : IntegrationMessage<TMessage>;
        void Dispose();
        object Get_consumers<TMessage, TConsumer>()
            where TMessage : IntegrationMessage<TMessage>
            where TConsumer : IConsumeMessage<TMessage>;
        void RegisterConsumer<TMessage, TConsumer>()
            where TMessage : IntegrationMessage<TMessage>
            where TConsumer : IConsumeMessage<TMessage>;
        void SendAck(ulong deliveryTag);
        void SendNack(ulong deliveryTag, bool requeue);
    }
}