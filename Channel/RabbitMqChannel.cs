using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TestRabbitClient.Dispatch;

namespace TestRabbitClient.Channel
{
    /// <summary>
    /// Default implementation of <see cref="IChannel" />,
    /// it will publish events to RabbitMQ
    /// </summary>
    public class RabbitMqChannel : IChannel
    {
        private readonly IConnectionFactoryWrapper _factory;
        private readonly ConcurrentDictionary<PublishStatus, int> _results;

        private const string MqExchangeName = "TestExchange";

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMqChannel"/> class.
        /// </summary>
        /// <param name="factory">Connection factory wrapper</param>
        /// <param name="configuration">Configuration instance</param>
        /// <param name="logger">Logger instance</param>
        public RabbitMqChannel(IConnectionFactoryWrapper factory, ConcurrentDictionary<PublishStatus, int> results)
        {
            _factory = factory;
            _results = results;


            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(MqExchangeName, ExchangeType.Topic, true);
            }
        }

        /// <inheritdoc/>
        public Task<PublishStatus> PublishAsync(string message)
        {
            return Task.Run(Impl);

            PublishStatus Impl()
            {
                try
                {
                    Publish(message);
                }
                catch (Exception)
                {
                    _results.AddOrUpdate(PublishStatus.Exception, 0, (k, v) => v+1);
                    return PublishStatus.Exception;
                }

                _results.AddOrUpdate(PublishStatus.Success, 0, (k, v) => v+1);
                return PublishStatus.Success;
            }
        }

        private void Publish(string message)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ConfirmSelect();

                byte[] byteMessage = Encoding.UTF8.GetBytes(message);

                string routingKey = "domain.test.1";
                channel.BasicPublish(MqExchangeName, routingKey, null, byteMessage);

                bool isConfirmed = channel.WaitForConfirms(new TimeSpan(0, 0, 5), out bool timedOut);
                if (!isConfirmed)
                {
                    throw new InvalidOperationException("Message didn't confirm");
                }

                if (timedOut)
                {
                    throw new InvalidOperationException("Message ran time out");
                }
            }
        }
    }
}