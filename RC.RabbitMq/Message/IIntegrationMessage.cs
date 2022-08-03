namespace RC.RabbitMq.Message
{
    public interface IIntegrationMessage
    {
        DateTime DateUtc { get; }
        public string MessageType { get; }
        Guid ID { get; set; }
    }

}
