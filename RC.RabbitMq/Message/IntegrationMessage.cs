namespace RC.RabbitMq.Message
{

    public abstract class IntegrationMessage : IIntegrationMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string MessageType { get; init; } = "";
        public DateTime CreateDate { get; private set; } = DateTime.UtcNow;
        public DateTime DateUtc { get; }
        public Guid ID { get; set; }

        public IntegrationMessage()
        {
            this.Id = Guid.NewGuid();
            this.CreateDate = DateTime.UtcNow;
        }
        public IntegrationMessage(Guid id, DateTime createDate)
        {
            this.Id = id;
            this.CreateDate = createDate;
        }
    }
}
