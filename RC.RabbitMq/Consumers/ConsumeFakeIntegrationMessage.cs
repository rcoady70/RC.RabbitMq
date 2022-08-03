using RabbitMQ.Client;
using RC.RabbitMq.Message;

namespace RC.RabbitMq.Consumers
{
    public class ConsumeFakeIntegrationMessage : DefaultBasicConsumer, IConsumeMessage<FakeIntegrationMessage>
    {
        public void ProcessMessage(FakeIntegrationMessage message)
        {
            using StreamWriter file = new($"{Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.IndexOf("bin") - 1)}\\ConsumerFakeMessage.txt", append: true);
            file.WriteLine(message.Email);
            file.Flush();
            file.Close();
        }
    }
}
