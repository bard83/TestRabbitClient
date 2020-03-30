using System.Threading.Tasks;

namespace TestRabbitClient.Dispatch
{
    /// <summary>
    /// Interface that define the handle operation for the message
    /// </summary>
    public interface IChannel
    {
        /// <summary>
        /// Publishes the message passed from the Dispatcher
        /// </summary>
        /// <param name="message">message to be published</param>
        /// <returns>status of operation</returns>
        Task<PublishStatus> PublishAsync(string message);
    }
}
