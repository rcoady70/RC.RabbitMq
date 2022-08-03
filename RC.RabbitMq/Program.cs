using RC.MessageQueue;
using RC.RabbitMq.Consumers;
using RC.RabbitMq.Message;


//Connect to rabbitMQ
using RabbitMQueueManager rmq = new RabbitMQueueManager("amqp://guest:guest@localhost:5672/");

///Register message and its consumer. 
/// Exchange is created and is named "TMessage-Ex" FakeIntegrationMessage-Ex 
/// Queue is created and named "TMessage-TConsumer-Q" eg FakeIntegrationMessage-ConsumeFakeIntegrationMessage-Q 
///
rmq.RegisterConsumer<FakeIntegrationMessage, ConsumeFakeIntegrationMessage>();
////RegisteredWaitHandle second consumer of the message
rmq.RegisterConsumer<FakeIntegrationMessage, ConsumeFakeIntegrationMessage1>();
//
rmq.RegisterConsumer<SendEmailIntegrationMessage, ConsumeSendEmailMessage>();


////Add messages to exchange
////
for (int i = 1; i <= 10; i++)
{
    var qMessage = new FakeIntegrationMessage();
    qMessage.Email = $"{i}email@gmailONE.com";
    //Messages are added to exchange FakeIntegrationMessage-Ex, exchange is generated based on message typeof(FakeIntegrationMessage).Name + "-Ex"
    rmq.AddMessageToQueue<FakeIntegrationMessage>(qMessage);
}
// Send email message 
//
var emailMessage = new SendEmailIntegrationMessage();
emailMessage.Email = $"tomsmith@gmailONE.com";
rmq.AddMessageToQueue<SendEmailIntegrationMessage>(emailMessage);

Console.WriteLine("Done");
Console.ReadLine();
