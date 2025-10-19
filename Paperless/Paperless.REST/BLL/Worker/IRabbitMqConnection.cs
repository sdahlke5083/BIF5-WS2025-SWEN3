using RabbitMQ.Client;

namespace Paperless.REST.BLL.Worker
{
    /// <summary>
    /// Represents a connection to RabbitMQ and manages its lifecycle.
    /// </summary>
    public interface IRabbitMqConnection : IAsyncDisposable
    {
        /// <summary>
        /// Gets the current RabbitMQ connection. Throws an exception if the connection has not been established.
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Gets the communication channel associated with the current instance. Throws an exception if the channel has not been created.
        /// </summary>
        IChannel Channel { get; }

        /// <summary>
        /// Creates a new RabbitMQ channel (model) for publishing and consuming messages.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the channel creation operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that completes with a newly created <see cref="IChannel"/>,
        /// which represents the channel to use for RabbitMQ operations.
        /// </returns>
        Task<IChannel> CreateChannelAsync(CancellationToken ct = default);
    }
}
