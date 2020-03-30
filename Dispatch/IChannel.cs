using System.Threading.Tasks;

namespace TestRabbitClient.Dispatch
{
    /// <summary>
    /// Interface that define the handle operation foa notarized event
    /// </summary>
    public interface IChannel
    {
        /// <summary>
        /// Publishes the message passed from the Dispatcher
        /// </summary>
        /// <param name="ne">message to be published</param>
        /// <returns>status of operation</returns>
        Task<PublishStatus> PublishAsync(string message);
    }
}
