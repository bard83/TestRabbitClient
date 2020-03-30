using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestRabbitClient.Channel;
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

        private class BarrieredMqChannel : IChannel
        {
            private readonly Barrier _barrier;
            private readonly IChannel _channel;

            public BarrieredMqChannel(Barrier barrier, IChannel mqChannel)
            {
                _barrier = barrier;
                _channel = mqChannel;
            }

            public Task<PublishStatus> PublishAsync(string ne)
            {
                return Task.Run(Impl);
                Task<PublishStatus> Impl()
                {
                    _barrier.SignalAndWait();
                    return _channel.PublishAsync(ne);
                }
            }
        }

        [Test]
        public async Task PublishAsync_WhenConcurrent_ReturnsOk()
        {
            // Arrange
            var barrier = new Barrier(2);
            var rabbitFactoryWrapper1 = new RbConnectionFactoryWrapper();
            var rabbitFactoryWrapper2 = new RbConnectionFactoryWrapper();
            var rbChannel1 = new RabbitMqChannel(rabbitFactoryWrapper1);
            var rbChannel2 = new RabbitMqChannel(rabbitFactoryWrapper2);
            var barrierChannel1 = new BarrieredMqChannel(barrier, rbChannel1);
            var barrierChannel2 = new BarrieredMqChannel(barrier, rbChannel2);

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
            var rabbitFactoryWrapper = new RbConnectionFactoryWrapper();
            var rbChannel = new RabbitMqChannel(rabbitFactoryWrapper);

            for (int i = 0; i < numberOfMessages; i++)
            {
                var barrierChannel = new BarrieredMqChannel(barrier, rbChannel);

                // Act
                taskList.Add(rbChannel.PublishAsync($"My message {DateTime.Now.ToString("o")}"));
            }

            await Task.WhenAll(taskList).ConfigureAwait(false);

            // Assert
            taskList.Where(t => t.Result == PublishStatus.Success).Count().Should().Be(numberOfMessages);
        }
    }
}
