using RabbitMQ.Client;

namespace TestRabbitClient.Channel
{
    /// <summary>
    /// Interface that wraps RabbitMQ connection factory
    /// </summary>
    public interface IConnectionFactoryWrapper
    {
        /// <summary>
        /// Creates a connection
        /// </summary>
        /// <returns><see cref="IConnection"/> instance</returns>
        IConnection CreateConnection();
    }
}
