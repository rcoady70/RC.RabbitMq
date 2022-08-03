namespace RC.RabbitMq.Message
{

    public abstract class IntegrationMessage<T>
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
            //Required to de-serialize message into correct derived type
            this.MessageType = this.GetType().FullName;
        }
        public IntegrationMessage(Guid id, DateTime createDate) : base()
        {
            this.Id = id;
            this.CreateDate = createDate;
        }
    }
}
