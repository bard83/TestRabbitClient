using System.Threading.Tasks;

namespace TestRabbitClient.Dispatch
{
    /// <summary>
    /// Interface for dispatching messages.
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Will execute publishing the message to all registered handlers.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <returns>Status in case of error</returns>
        Task<DispatchStatus> DispatchAsync(string message);
    }
}
