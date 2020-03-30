using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestRabbitClient.Dispatch;

namespace TestRabbitClient.Tests
{
#pragma warning disable SA1201
    public class RabbitMqChannelUnitTests
    {
        private static int[] numberOfMessages = new int[]
        {
            100,
        };

        private class MqChannel : IChannel
        {
            private readonly ConnectionFactory _factory;
            private readonly IConfiguration _configuration;

            private const string MqExchangeName = "TestExchange";
            private readonly IConnection _connection;

            public MqChannel()
            {
                _configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables("RB_")
                    .Build();
                _factory = new ConnectionFactory
                {
                    UserName = "rabbitmq",
                    Password = "rabbitmq",
                    HostName = _configuration.GetValue<string>("MQ_SERVER"),
                    Port = 5672,
                    VirtualHost = "/",
                };


                _connection = _factory.CreateConnection();
                using (var channel = _connection.CreateModel())
                {
                    channel.ExchangeDeclare(MqExchangeName, ExchangeType.Topic, true);
                }
            }

            public Task<PublishStatus> PublishAsync(string message)
            {
                return Task.Run(Impl);
                PublishStatus Impl()
                {
                    try
                    {
                        Publish(message);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Error occurred : {0}", e.ToString());
                        return PublishStatus.Exception;
                    }

                    return PublishStatus.Success;
                }
            }

            private void Publish(string message)
            {
                using (var channel = _connection.CreateModel())
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

        private class BarrierDispatcher : IDispatcher
        {
            private readonly Barrier _barrier;
            private readonly IEnumerable<IChannel> _eventHandlers;

            public BarrierDispatcher(Barrier barrier, IEnumerable<IChannel> eventHandlers)
            {
                _barrier = barrier;
                _eventHandlers = eventHandlers;
            }

            public Task<DispatchStatus> DispatchAsync(string message)
            {
                return Task.Run(Impl);
                async Task<DispatchStatus> Impl()
                {
                    _barrier.SignalAndWait();
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

        [Test]
        public async Task PublishAsync_WhenConcurrent_ReturnsOk()
        {
            // Arrange
            var barrier = new Barrier(2);
            var barrierChannel1 = new MqChannel();
            var barrierChannel2 = new MqChannel();

            // Act
            var t1 = barrierChannel1.PublishAsync($"My message {DateTime.Now.ToString("o")}");
            var t2 = barrierChannel2.PublishAsync($"My message {DateTime.Now.ToString("o")}");

            await Task.WhenAll(new Task[] { t1, t2 }).ConfigureAwait(false);

            // Assert
            t1.Result.Should().Be(PublishStatus.Success);
            t2.Result.Should().Be(PublishStatus.Success);
        }

        [TestCaseSource(nameof(numberOfMessages))]
        public async Task PublishAsync_WhenConcurrentWithInput_ReturnsOk(int numberOfMessages)
        {
            // Arrange
            var barrier = new Barrier(numberOfMessages);
            var taskList = new List<Task<PublishStatus>>();
            var mqChannel = new MqChannel();

            // Act
            for (int i = 0; i < numberOfMessages; i++)
            {
                taskList.Add(mqChannel.PublishAsync($"My message {DateTime.Now.ToString("o")}"));
            }

            await Task.WhenAll(taskList).ConfigureAwait(false);

            // Assert
            taskList.Where(t => t.Result == PublishStatus.Success).Count().Should().Be(numberOfMessages);
        }

        [TestCaseSource(nameof(numberOfMessages))]
        public async Task DispatchAsync_WhenMultiConcurrent_ReturnsOk(int size)
        {
            // Arrange
            var barrier = new Barrier(size);
            var mqChannel = new MqChannel();
            var channels = new List<IChannel>
            {
                mqChannel,
            };
            var dispatcher = new BarrierDispatcher(barrier, channels);

            // Act
            var tasks = new List<Task<DispatchStatus>>();
            for (int i = 0; i < size; i++)
            {
                var t = dispatcher.DispatchAsync($"My message {DateTime.Now.ToString("o")}");
                tasks.Add(t);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Assert
            var contentTask = tasks.Where(t => t.Result != DispatchStatus.Success).ToList();
            contentTask.Should().BeEmpty();
        }
    }
}
