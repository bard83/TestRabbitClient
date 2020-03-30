using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestRabbitClient.Dispatch
{
    /// <summary>
    /// Implementation of <see cref="IDispatcher"/> which publishes messages to registered <see cref="IChannel"/>-s.
    /// </summary>
    public class Dispatcher : IDispatcher
    {
        private readonly IEnumerable<IChannel> _eventHandlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dispatcher"/> class.
        /// </summary>
        /// <param name="eventHandlers">List of IEventListeners.</param>
        public Dispatcher(IEnumerable<IChannel> eventHandlers)
        {
            _eventHandlers = eventHandlers;
        }

        /// <inheritdoc/>
        public async Task<DispatchStatus> DispatchAsync(string message)
        {
            foreach (var handler in _eventHandlers)
            {
                var res = await handler.PublishAsync(message).ConfigureAwait(false);
                if (res != PublishStatus.Success)
                {
                    // We do not report these errors to the users, the dispatch process itself
                    // is supposed to be decoupled from other operations.
                    Console.Error.WriteLine("Handler had error: {0}", res);
                }
            }

            return DispatchStatus.Success;
        }
    }
}
