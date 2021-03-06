namespace TestRabbitClient.Dispatch
{
    /// <summary>
    /// Possible statuses of an <see cref="M:IDispatcher.Dispatch"/> operation.
    /// </summary>
    public enum DispatchStatus
    {
        /// <summary>
        /// message dispatched successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Placeholder for unimplmented operations.
        /// </summary>
        NotImplemented,
    }
}
