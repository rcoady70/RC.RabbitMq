using RC.RabbitMq.Message;

namespace RC.RabbitMq.Consumers
{
    public interface IConsumeMessage
    {
        void ProcessMessage(FakeIntegrationMessage message);
    }
}