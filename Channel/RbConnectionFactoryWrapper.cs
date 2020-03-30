using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;

namespace TestRabbitClient.Channel
{
    /// <summary>
    /// Default implementation for <see cref="IConnectionFactoryWrapper"/>,
    /// wraps the rabbit mq connection factory and creates connections
    /// </summary>
    public class RbConnectionFactoryWrapper : IConnectionFactoryWrapper
    {
        private readonly ConnectionFactory _factory;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="RbConnectionFactoryWrapper"/> class.
        /// </summary>
        /// <param name="configuration">Configuration instance</param>
        /// <param name="logger">Logger instance</param>
        public RbConnectionFactoryWrapper()
        {
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables("RB_")
                .Build();
            _factory = GetFactory();
        }

        /// <inheritdoc />
        public IConnection CreateConnection()
        {
            return _factory.CreateConnection();
        }

        private ConnectionFactory GetFactory()
        {
            return new ConnectionFactory
            {
                UserName = "rabbitmq",
                Password = "rabbitmq",
                HostName = _configuration.GetValue<string>("MQ_SERVER"),
                Port = 5672,
                VirtualHost = "/",
            };
        }
    }
}
