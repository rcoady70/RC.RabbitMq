using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RC.RabbitMq.Consumers;
using RC.RabbitMq.Exceptions;
using RC.RabbitMq.Message;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace RC.MessageQueue
{
    public class RabbitMQueueManager : IDisposable, IRabbitMQueueManager
    {
        //Consumers for each queue
        private System.Collections.Generic.Dictionary<string, System.Type> _consumers = new System.Collections.Generic.Dictionary<string, System.Type>();
        private const string _deadLetterQueueName = "Dead-Letter-Q";
        private const string _deadLetterExchangeName = "Dead-Letter-Ex";
        private const string _defaultQueueName = "Default-Q";
        private const string _defaultExchangeName = "Default-Letter-Ex";
        private readonly IModel _channel; //Connection to write messages exchange
        private readonly IModel _consumerChannel; //Connection used to consume messages
        private readonly IConnection _connection;
        private readonly IConnection _consumerConnection;
        private bool _disposed = false;

        public RabbitMQueueManager(string connectionstring)
        {
            var factory = new ConnectionFactory() { Uri = new Uri(connectionstring), DispatchConsumersAsync = true };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _consumerConnection = factory.CreateConnection(); ;
            _consumerChannel = _consumerConnection.CreateModel();
            //Create dead letter exchange
            //
            CreateDeadDetterExchangeQueue();
            CreateDefaultExchangeQueue();
        }


        private void CreateDeadDetterExchangeQueue()
        {
            //Declare exchange
            _channel.ExchangeDeclare(exchange: _deadLetterExchangeName,
                                     type: ExchangeType.Fanout,
                                     durable: false,
                                     autoDelete: true
                                    );
            //Declare queue
            _channel.QueueDeclare(queue: _deadLetterQueueName,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            //Bind queue and exchange
            _channel.QueueBind(queue: _deadLetterQueueName,
                               exchange: _deadLetterExchangeName,
                               routingKey: "");
        }
        /// <summary>
        /// Create catch all default exchange
        /// </summary>
        private void CreateDefaultExchangeQueue()
        {
            //Declare exchange
            _channel.ExchangeDeclare(exchange: _defaultExchangeName,
                                     type: ExchangeType.Fanout,
                                     durable: false,
                                     autoDelete: true
                                    );
            //Declare queue
            _channel.QueueDeclare(queue: _defaultQueueName,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            //Bind queue and exchange
            _channel.QueueBind(queue: _defaultQueueName,
                               exchange: _defaultExchangeName,
                               routingKey: "");
        }
        /// <summary>
        /// Publish message to RABBIT MQ. Message will be written to exchange TMessage.Name-Ex
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        public void AddMessageToQueue<TMessage>(TMessage message)
                    where TMessage : IntegrationMessage
        {
            var exchange = $"{typeof(TMessage).Name}-Ex";
            //Use type to ensure derived properties are serialized.
            var serializedMessage = JsonSerializer.Serialize(message, message.GetType());

            var messageProperties = _channel.CreateBasicProperties();
            messageProperties.ContentType = "application/json";
            messageProperties.Headers = new Dictionary<string, object>();
            messageProperties.Headers.Add("message-type", typeof(TMessage).FullName);
            messageProperties.Expiration = "60000";

            _channel.BasicPublish(exchange: exchange,
                                  routingKey: $"",
                                  basicProperties: messageProperties,
                                  body: Encoding.UTF8.GetBytes(serializedMessage));
        }

        public object Get_consumers<TMessage, TConsumer>()
            where TMessage : IntegrationMessage
            where TConsumer : IConsumeMessage
        {
            return _consumers;
        }

        /// <summary>
        /// Register message and its consumer. Exchanges is named "TMessage-Ex" Queue is named "TMessage-TConsumer-Q"
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <typeparam name="TConsumer"></typeparam>
        /// <param name="queueName"></param>
        /// <exception cref="ArgumentException"></exception>
        public void RegisterConsumer<TMessage, TConsumer>()
                                    where TMessage : IntegrationMessage
                                    where TConsumer : IConsumeMessage
        {
            //Create exchange and queue based on the names of TMessage, TConsumer
            //
            var queueName = CreateExchangeQueue<TMessage, TConsumer>();

            var consumerType = typeof(TConsumer);
            if (!_consumers.ContainsKey(queueName))
            {
                //Setup consumer
                var messageConsumer = new AsyncEventingBasicConsumer(_consumerChannel);
                messageConsumer.Received += Consumer_Received;
                _consumerChannel.BasicConsume(queue: queueName,
                                              autoAck: false,
                                              consumer:
                                              messageConsumer,
                                              consumerTag: queueName);

                //Keep list of messages and consumers
                _consumers.Add(queueName, consumerType);
            }
            else
                throw new RabbitMQNoConsumerFound($"Only one consumer allowed per RabbitMQ queue {queueName}. Registering message : {typeof(TMessage).Name} handler {typeof(TConsumer).Name}");
        }
        /// <summary>
        /// Get death count for message to ensure it does not loop indefinably
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private int GetDeathCount(IBasicProperties properties)
        {
            if (properties.Headers.ContainsKey("x-death"))
            {
                var deathProperties = (List<object>)properties.Headers["x-death"];
                var lastRetry = (Dictionary<string, object>)deathProperties[0];
                var count = lastRetry["count"];
                return (int)count;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// Generic consumer receive event 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var routingKey = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

            try
            {
                var deathCount = GetDeathCount(eventArgs.BasicProperties);
                await ProcessEvent(routingKey, message, eventArgs);
                SendAck(eventArgs.DeliveryTag);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED {eventArgs.DeliveryTag} " + ex.Message);
                if (GetDeathCount(eventArgs.BasicProperties) <= 5)
                    SendNack(eventArgs.DeliveryTag, true);
                else
                    SendNack(eventArgs.DeliveryTag, false);
            }

            // Even on exception we take the message off the queue.
            // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
            // For more information see: https://www.rabbitmq.com/dlx.html
            //_consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        private async Task ProcessEvent(string routingKey, string message, BasicDeliverEventArgs eventArgs)
        {
            //De serialize message based on message type in the header
            string msgType = Encoding.UTF8.GetString((byte[])eventArgs.BasicProperties.Headers["message-type"]);
            var desearlizedMSG = (IIntegrationMessage)JsonSerializer.Deserialize(message, Type.GetType(msgType));

            if (_consumers.ContainsKey(eventArgs.ConsumerTag))
            {
                //Hydrate message to process 
                Type consumerType = _consumers[eventArgs.ConsumerTag];
                var consumerInstance = Activator.CreateInstance(consumerType, new object[] { });

                //Spin up message consumer 
                MethodInfo methodInfo = consumerType.GetMethod("ProcessMessage");
                methodInfo.Invoke(consumerInstance, new object[] { desearlizedMSG });
                Console.WriteLine($"{eventArgs.ConsumerTag} - {consumerType.Name} ");
            }
            else
                throw new RabbitMQNoConsumerFound($"Consumer not found for {eventArgs.ConsumerTag} message-type {msgType} ");

            await Task.Yield();
        }
        /// <summary>
        /// Create and bind queue and exchange. One consumer per queue. Exchanges named typeof(TMessage).Name-Exchange queue named typeof(TMessage).Name-Queue
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <typeparam name="TConsumer"></typeparam>
        /// <param name="ttl"></param>
        /// <returns></returns>
        private string CreateExchangeQueue<TMessage, TConsumer>(int ttl = 600000000)
        {
            var exchange = $"{typeof(TMessage).Name}-Ex";
            var queue = $"{typeof(TMessage).Name}-{typeof(TConsumer).Name}-Q";
            //Declare exchange
            _channel.ExchangeDeclare(exchange: exchange,
                                     type: ExchangeType.Direct,
                                     durable: false,
                                     autoDelete: true,
                                     arguments: new Dictionary<string, object>()
                                         {
                                            {"alternate-exchange" , _defaultExchangeName}
                                         }
                                    );
            //Declare queue
            _channel.QueueDeclare(queue: queue,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: new Dictionary<string, object>()
                                  {
                                      {"x-dead-letter-exchange" , _deadLetterExchangeName}
                                      //{"x-message-ttl" , ttl}
                                  });
            //Bind queue and exchange
            _channel.QueueBind(queue: queue,
                               exchange: exchange,
                               routingKey: "");
            return queue;
        }
        /// <summary>
        /// Send positive ack
        /// </summary>
        /// <param name="deliveryTag"></param>
        public void SendAck(ulong deliveryTag)
        {
            _consumerChannel.BasicAck(deliveryTag: deliveryTag, multiple: false);
        }
        /// <summary>
        /// Send negative ack
        /// </summary>
        /// <param name="deliveryTag"></param>
        /// <param name="requeue"></param>
        public void SendNack(ulong deliveryTag, bool requeue)
        {
            _consumerChannel.BasicNack(deliveryTag: deliveryTag, multiple: false, false);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_channel != null)
                {
                    if (!_channel.IsClosed)
                    {
                        _channel.Close();
                        _channel.Dispose();
                    }
                }
                if (_consumerChannel != null)
                {
                    if (!_consumerChannel!.IsClosed)
                    {
                        _consumerChannel.Close();
                        _consumerChannel.Dispose();
                    }
                }
                if (_connection != null)
                {
                    if (_connection.IsOpen)
                        _connection.Close();
                }
                if (_consumerConnection != null)
                {
                    if (_consumerConnection.IsOpen)
                        _consumerConnection.Close();
                }
                _disposed = true;
            }
        }
    }
}
