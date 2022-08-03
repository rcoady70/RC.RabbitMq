namespace RC.RabbitMq.Message
{
    public class SendEmailIntegrationMessage : IntegrationMessage<SendEmailIntegrationMessage>
    {
        public string Email { get; set; }
    }

}
