namespace RC.RabbitMq.Message
{
    public class FakeIntegrationMessage : IntegrationMessage<FakeIntegrationMessage>
    {
        public string Email { get; set; }
    }

}
