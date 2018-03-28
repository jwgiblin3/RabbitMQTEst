using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQExample
{
    class Program
    {
        static string host = System.Environment.MachineName;
        static string queue = "FAP_" + host;

        static void Main(string[] args)
        {

            var rpcClient = new RpcClient();

            Console.WriteLine(" [x] Requesting fib(x)");
            while(true)
            {
                var input = Console.ReadLine();
            
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                int Intvalue;
                if (int.TryParse(input, out Intvalue))
                {
                    var response = rpcClient.Call(input);
                    Console.WriteLine(" [.] Got '{0}'", response);
                }
                else Console.WriteLine("try Again");
            }
            rpcClient.Close();
        }
        public class RpcClient
        {
            private readonly IConnection connection;
            private readonly IModel channel;
            private readonly string replyQueueName;
            private readonly EventingBasicConsumer consumer;
            private readonly BlockingCollection<string> respQueue = new BlockingCollection<string>();
            private readonly IBasicProperties props;

            public RpcClient()
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };

                connection = factory.CreateConnection();
                channel = connection.CreateModel();
                replyQueueName = channel.QueueDeclare().QueueName;
                consumer = new EventingBasicConsumer(channel);

                props = channel.CreateBasicProperties();
                var correlationId = Guid.NewGuid().ToString();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyQueueName;

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var response = Encoding.UTF8.GetString(body);
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        respQueue.Add(response);
                    }
                };
            }

            public string Call(string message)
            {

                var messageBytes = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(
                    exchange: "",
                    routingKey: queue,
                    basicProperties: props,
                    body: messageBytes);


                channel.BasicConsume(
                    consumer: consumer,
                    queue: replyQueueName,
                    autoAck: true);

                return respQueue.Take(); ;
            }

            public void Close()
            {
                connection.Close();
            }
        }
    }
}
