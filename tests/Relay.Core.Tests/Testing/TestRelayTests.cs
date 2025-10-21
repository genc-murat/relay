 using System;
 using System.Collections.Generic;
 using System.Linq;
 using System.Threading;
 using System.Threading.Tasks;
 using Relay.Core.Contracts.Core;
 using Relay.Core.Contracts.Requests;
 using Relay.Core.Testing;
 using Xunit;

namespace Relay.Core.Tests.Testing
{
    public class TestRelayTests
    {
        private readonly TestRelay _relay;

        public TestRelayTests()
        {
            _relay = new TestRelay();
        }

        [Fact]
        public void PublishedNotifications_ReturnsEmptyCollection_WhenNoNotificationsPublished()
        {
            // Act
            var notifications = _relay.PublishedNotifications;

            // Assert
            Assert.Empty(notifications);
        }

        [Fact]
        public void SentRequests_ReturnsEmptyCollection_WhenNoRequestsSent()
        {
            // Act
            var requests = _relay.SentRequests;

            // Assert
            Assert.Empty(requests);
        }

        [Fact]
        public async Task SendAsync_Generic_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _relay.SendAsync<string>(null!).AsTask());
        }

        [Fact]
        public async Task SendAsync_Generic_WithoutHandler_ReturnsDefaultValue()
        {
            // Arrange
            var request = new TestRequest();

            // Act
            var result = await _relay.SendAsync<string>(request);

            // Assert
            Assert.Equal(default(string), result);
            Assert.Single(_relay.SentRequests);
            Assert.Equal(request, _relay.SentRequests.Single());
        }

        [Fact]
        public async Task SendAsync_Generic_WithHandler_ReturnsHandlerResult()
        {
            // Arrange
            var request = new TestRequest();
            var expectedResponse = "test response";
            _relay.SetupRequestHandler<TestRequest, string>((r, ct) => ValueTask.FromResult(expectedResponse));

            // Act
            var result = await _relay.SendAsync<string>(request);

            // Assert
            Assert.Equal(expectedResponse, result);
            Assert.Single(_relay.SentRequests);
            Assert.Equal(request, _relay.SentRequests.Single());
        }

        [Fact]
        public async Task SendAsync_Generic_WithHandler_ThrowsException_PropagatesException()
        {
            // Arrange
            var request = new TestRequest();
            var expectedException = new InvalidOperationException("Test error");
            _relay.SetupRequestHandler<TestRequest, string>((r, ct) => new ValueTask<string>(Task.FromException<string>(expectedException)));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _relay.SendAsync<string>(request).AsTask());

            Assert.Equal(expectedException, exception);
            Assert.Single(_relay.SentRequests);
            Assert.Equal(request, _relay.SentRequests.Single());
        }

        [Fact]
        public async Task SendAsync_NonGeneric_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _relay.SendAsync(null!).AsTask());
        }

        [Fact]
        public async Task SendAsync_NonGeneric_WithoutHandler_CompletesSuccessfully()
        {
            // Arrange
            var request = new TestFireAndForgetRequest();

            // Act
            await _relay.SendAsync(request);

            // Assert
            Assert.Single(_relay.SentRequests);
            Assert.Equal(request, _relay.SentRequests.Single());
        }

        [Fact]
        public async Task SendAsync_NonGeneric_WithHandler_ExecutesHandler()
        {
            // Arrange
            var request = new TestFireAndForgetRequest();
            var handlerExecuted = false;
            _relay.SetupRequestHandler<TestFireAndForgetRequest>((r, ct) =>
            {
                handlerExecuted = true;
                return ValueTask.CompletedTask;
            });

            // Act
            await _relay.SendAsync(request);

            // Assert
            Assert.True(handlerExecuted);
            Assert.Single(_relay.SentRequests);
            Assert.Equal(request, _relay.SentRequests.Single());
        }

        [Fact]
        public async Task StreamAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var item in _relay.StreamAsync<string>(null!)) { }
            });
        }

        [Fact]
        public async Task StreamAsync_WithoutHandler_ReturnsEmptyEnumerable()
        {
            // Arrange
            var request = new TestStreamRequest();

            // Act
            var results = new List<string>();
            await foreach (var item in _relay.StreamAsync<string>(request))
            {
                results.Add(item);
            }

            // Assert
            Assert.Empty(results);
            Assert.Single(_relay.SentRequests);
            Assert.Equal(request, _relay.SentRequests.Single());
        }

        [Fact]
        public async Task StreamAsync_WithHandler_ReturnsHandlerResults()
        {
            // Arrange
            var request = new TestStreamRequest();
            var expectedResults = new[] { "item1", "item2", "item3" };
            _relay.SetupStreamHandler<TestStreamRequest, string>((r, ct) =>
                EnumerateItemsAsync(expectedResults));

            // Act
            var results = new List<string>();
            await foreach (var item in _relay.StreamAsync<string>(request))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(expectedResults, results);
            Assert.Single(_relay.SentRequests);
            Assert.Equal(request, _relay.SentRequests.Single());
        }

        [Fact]
        public async Task PublishAsync_WithNullNotification_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _relay.PublishAsync<TestNotification>(null!).AsTask());
        }

        [Fact]
        public async Task PublishAsync_WithoutHandler_CompletesSuccessfully()
        {
            // Arrange
            var notification = new TestNotification();

            // Act
            await _relay.PublishAsync(notification);

            // Assert
            Assert.Single(_relay.PublishedNotifications);
            Assert.Equal(notification, _relay.PublishedNotifications.Single());
        }

        [Fact]
        public async Task PublishAsync_WithHandler_ExecutesHandler()
        {
            // Arrange
            var notification = new TestNotification();
            var handlerExecuted = false;
            _relay.SetupNotificationHandler<TestNotification>((n, ct) =>
            {
                handlerExecuted = true;
                return ValueTask.CompletedTask;
            });

            // Act
            await _relay.PublishAsync(notification);

            // Assert
            Assert.True(handlerExecuted);
            Assert.Single(_relay.PublishedNotifications);
            Assert.Equal(notification, _relay.PublishedNotifications.Single());
        }

        [Fact]
        public async Task SetupRequestHandler_Generic_AddsHandlerToDictionary()
        {
            // Arrange
            var handlerExecuted = false;
            Func<TestRequest, CancellationToken, ValueTask<string>> handler = (r, ct) =>
            {
                handlerExecuted = true;
                return ValueTask.FromResult("response");
            };

            // Act
            _relay.SetupRequestHandler<TestRequest, string>(handler);

            // Assert - Handler should be executed when request is sent
            var request = new TestRequest();
            await _relay.SendAsync<string>(request);
            Assert.True(handlerExecuted);
        }

        [Fact]
        public async Task SetupRequestHandler_NonGeneric_AddsHandlerToDictionary()
        {
            // Arrange
            var handlerExecuted = false;
            Func<TestFireAndForgetRequest, CancellationToken, ValueTask> handler = (r, ct) =>
            {
                handlerExecuted = true;
                return ValueTask.CompletedTask;
            };

            // Act
            _relay.SetupRequestHandler<TestFireAndForgetRequest>(handler);

            // Assert - Handler should be executed when request is sent
            var request = new TestFireAndForgetRequest();
            await _relay.SendAsync(request);
            Assert.True(handlerExecuted);
        }

        [Fact]
        public async Task SetupStreamHandler_AddsHandlerToDictionary()
        {
            // Arrange
            var handlerExecuted = false;
            Func<TestStreamRequest, CancellationToken, IAsyncEnumerable<string>> handler = (r, ct) =>
            {
                handlerExecuted = true;
                return EnumerateItemsAsync(new[] { "item" });
            };

            // Act
            _relay.SetupStreamHandler<TestStreamRequest, string>(handler);

            // Assert - Handler should be executed when request is sent
            var request = new TestStreamRequest();
            await foreach (var item in _relay.StreamAsync<string>(request))
            {
                break;
            }
            Assert.True(handlerExecuted);
        }

        [Fact]
        public async Task SetupNotificationHandler_AddsHandlerToDictionary()
        {
            // Arrange
            var handlerExecuted = false;
            Func<TestNotification, CancellationToken, ValueTask> handler = (n, ct) =>
            {
                handlerExecuted = true;
                return new ValueTask(Task.CompletedTask);
            };

            // Act
            _relay.SetupNotificationHandler<TestNotification>(handler);

            // Assert - Handler should be executed when notification is published
            var notification = new TestNotification();
            await _relay.PublishAsync<TestNotification>(notification);
            Assert.True(handlerExecuted);
        }

        [Fact]
        public void Clear_RemovesAllRecordedRequestsAndNotifications()
        {
            // Arrange
            var request = new TestRequest();
            var notification = new TestNotification();
            _relay.SendAsync(request).AsTask().Wait();
            _relay.PublishAsync(notification).AsTask().Wait();

            // Act
            _relay.Clear();

            // Assert
            Assert.Empty(_relay.SentRequests);
            Assert.Empty(_relay.PublishedNotifications);
        }

        [Fact]
        public async Task ClearHandlers_RemovesAllRegisteredHandlers()
        {
            // Arrange
            _relay.SetupRequestHandler<TestRequest, string>((r, ct) => ValueTask.FromResult("response"));
            _relay.SetupNotificationHandler<TestNotification>((n, ct) => ValueTask.CompletedTask);

            // Act
            _relay.ClearHandlers();

            // Assert - Handlers should no longer be executed
            var request = new TestRequest();
            var notification = new TestNotification();
            var requestResult = await _relay.SendAsync<string>(request);
            await _relay.PublishAsync(notification);

            // Should return default values since handlers were cleared
            Assert.Equal(default(string), requestResult);
        }

        [Fact]
        public void GetPublishedNotifications_ReturnsTypedNotifications()
        {
            // Arrange
            var notification1 = new TestNotification();
            var notification2 = new TestNotification();
            var otherNotification = new OtherNotification();
            _relay.PublishAsync(notification1).AsTask().Wait();
            _relay.PublishAsync(notification2).AsTask().Wait();
            _relay.PublishAsync(otherNotification).AsTask().Wait();

            // Act
            var testNotifications = _relay.GetPublishedNotifications<TestNotification>();

            // Assert
            Assert.Equal(2, testNotifications.Count());
            Assert.Contains(notification1, testNotifications);
            Assert.Contains(notification2, testNotifications);
        }

        [Fact]
        public void GetSentRequests_ReturnsTypedRequests()
        {
            // Arrange
            var request1 = new TestRequest();
            var request2 = new TestRequest();
            var otherRequest = new TestFireAndForgetRequest();
            _relay.SendAsync(request1).AsTask().Wait();
            _relay.SendAsync(request2).AsTask().Wait();
            _relay.SendAsync(otherRequest).AsTask().Wait();

            // Act
            var testRequests = _relay.GetSentRequests<TestRequest>();

            // Assert
            Assert.Equal(2, testRequests.Count());
            Assert.Contains(request1, testRequests);
            Assert.Contains(request2, testRequests);
        }

        // Helper method for async enumeration
        private static async IAsyncEnumerable<string> EnumerateItemsAsync(string[] items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }

        // Test classes
        private class TestRequest : IRequest<string> { }

        private class TestFireAndForgetRequest : IRequest { }

        private class TestStreamRequest : IStreamRequest<string> { }

        private class TestNotification : INotification { }

        private class OtherNotification : INotification { }
    }
}