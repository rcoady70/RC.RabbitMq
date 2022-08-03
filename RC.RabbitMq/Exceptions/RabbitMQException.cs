namespace RC.RabbitMq.Exceptions
{
    public class RabbitMQNoConsumerFound : Exception
    {
        public RabbitMQNoConsumerFound(string message) : base(message)
        {
        }

    }
}
