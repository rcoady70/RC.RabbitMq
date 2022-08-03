using RabbitMQ.Client;
using RC.RabbitMq.Message;

namespace RC.RabbitMq.Consumers
{
    public class ConsumeSendEmailMessage : DefaultBasicConsumer, IConsumeMessage<SendEmailIntegrationMessage>
    {
        public void ProcessMessage(SendEmailIntegrationMessage message)
        {

            using StreamWriter file = new($"{Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.IndexOf("bin") - 1)}\\ConsumerSendEmail.txt", append: true);
            file.WriteLine(message.Email);
            file.Flush();
            file.Close();
            Thread.Sleep(20);
        }
    }
}
