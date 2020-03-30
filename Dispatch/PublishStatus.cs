namespace TestRabbitClient.Dispatch
{
    /// <summary>
    /// Possible statuses of an <see cref="M:IChannel.PublishAsync"/> operation.
    /// </summary>
    public enum PublishStatus
    {
        /// <summary>
        /// <see cref="NotarizedEvent"/> published successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Got an exception while handling the NotarizedEvent.
        /// </summary>
        Exception,

        /// <summary>
        /// Placeholder for unimplmented operations.
        /// </summary>
        NotImplemented,
    }
}
