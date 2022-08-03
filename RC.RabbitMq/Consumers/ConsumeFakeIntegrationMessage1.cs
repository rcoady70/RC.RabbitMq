using RabbitMQ.Client;
using RC.RabbitMq.Message;

namespace RC.RabbitMq.Consumers
{
    public class ConsumeFakeIntegrationMessage1 : DefaultBasicConsumer, IConsumeMessage
    {
        public void ProcessMessage(FakeIntegrationMessage message)
        {
            //Send message to dead letter queue
            if (message.Email == "2email@gmailONE.com")
                throw new Exception($"EXCEPTION Processing fake message 1 - {message.Email}");

            Console.WriteLine($"Processing fake message 1 - {message.Email}");
            using StreamWriter file = new($"{Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.IndexOf("bin") - 1)}\\Queue1.txt", append: true);
            file.WriteLine(message.Email);
            file.Flush();
            file.Close();
            Thread.Sleep(20);
        }
    }
}
