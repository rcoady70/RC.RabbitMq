namespace RC.RabbitMq.Message
{
    public class FakeIntegrationMessage : IntegrationMessage
    {
        /// <summary>
        /// Construct message 
        /// </summary>
        /// <param name="routingKey"></param>
        public FakeIntegrationMessage()
        {
            //Required to de-serialize message into correct derived type
            this.MessageType = this.GetType().FullName;
        }
        public string Email { get; set; }
    }

}
