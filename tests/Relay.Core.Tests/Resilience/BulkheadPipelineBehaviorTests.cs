using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Resilience.Bulkhead;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Resilience;

public class BulkheadPipelineBehaviorTests
{
    private readonly Mock<ILogger<BulkheadPipelineBehavior<TestRequest, TestResponse>>> _mockLogger;
    private readonly Mock<IOptions<BulkheadOptions>> _mockOptions;

    public BulkheadPipelineBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<BulkheadPipelineBehavior<TestRequest, TestResponse>>>();
        _mockOptions = new Mock<IOptions<BulkheadOptions>>();
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Logger_Is_Null()
    {
        // Arrange
        var options = new BulkheadOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BulkheadPipelineBehavior<TestRequest, TestResponse>(null!, _mockOptions.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Options_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BulkheadPipelineBehavior<TestRequest, TestResponse>(_mockLogger.Object, null!));
    }

    [Fact]
    public async Task HandleAsync_Should_Execute_Without_Bulkhead_When_MaxConcurrency_Is_Zero()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 0 };
        _mockOptions.Setup(x => x.Value).Returns(options);
        var behavior = new BulkheadPipelineBehavior<TestRequest, TestResponse>(_mockLogger.Object, _mockOptions.Object);
        var request = new TestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task HandleAsync_Should_Execute_Without_Bulkhead_When_MaxConcurrency_Is_Negative()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = -1 };
        _mockOptions.Setup(x => x.Value).Returns(options);
        var behavior = new BulkheadPipelineBehavior<TestRequest, TestResponse>(_mockLogger.Object, _mockOptions.Object);
        var request = new TestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task HandleAsync_Should_Execute_Successfully_When_Within_Concurrency_Limits()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 2 };
        _mockOptions.Setup(x => x.Value).Returns(options);
        var mockLogger = new Mock<ILogger<BulkheadPipelineBehavior<OtherTestRequest, TestResponse>>>();
        var behavior = new BulkheadPipelineBehavior<OtherTestRequest, TestResponse>(mockLogger.Object, _mockOptions.Object);
        var request = new OtherTestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulkhead acquired for OtherTestRequest")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulkhead released for OtherTestRequest")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_BulkheadRejectedException_When_Concurrency_Limit_Exceeded()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 1, MaxWaitTime = TimeSpan.FromMilliseconds(100) };
        _mockOptions.Setup(x => x.Value).Returns(options);
        var mockLogger = new Mock<ILogger<BulkheadPipelineBehavior<RejectionTestRequest, TestResponse>>>();
        var behavior = new BulkheadPipelineBehavior<RejectionTestRequest, TestResponse>(mockLogger.Object, _mockOptions.Object);
        var request = new RejectionTestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };

        // Create a delegate that takes time to complete
        var tcs = new TaskCompletionSource<TestResponse>();
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(tcs.Task));

        // Act - Start first request (should acquire semaphore)
        var firstTask = behavior.HandleAsync(request, next, CancellationToken.None);

        // Wait a bit to ensure first request acquired the semaphore
        await Task.Delay(50);

        // Try second request (should be rejected)
        var secondTask = behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<BulkheadRejectedException>(() => secondTask.AsTask());

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulkhead rejection for RejectionTestRequest: max concurrency (1) exceeded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Complete the first request
        tcs.SetResult(response);
        var firstResult = await firstTask;
        Assert.Equal(response, firstResult);
    }

    [Fact]
    public async Task HandleAsync_Should_Release_Semaphore_When_Next_Delegate_Throws_Exception()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 1 };
        _mockOptions.Setup(x => x.Value).Returns(options);
        var mockLogger = new Mock<ILogger<BulkheadPipelineBehavior<ExceptionTestRequest, TestResponse>>>();
        var behavior = new BulkheadPipelineBehavior<ExceptionTestRequest, TestResponse>(mockLogger.Object, _mockOptions.Object);
        var request = new ExceptionTestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };

        // First call that throws exception
        var nextWithException = new RequestHandlerDelegate<TestResponse>(() =>
        {
            throw new InvalidOperationException("Test exception");
        });

        // Second call that succeeds
        var nextWithSuccess = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.HandleAsync(request, nextWithException, CancellationToken.None).AsTask());

        // Semaphore should be released, so this should work
        var result = await behavior.HandleAsync(request, nextWithSuccess, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task HandleAsync_Should_Share_Semaphores_Across_Different_Behavior_Instances()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 1, MaxWaitTime = TimeSpan.FromMilliseconds(100) };
        var mockOptions1 = new Mock<IOptions<BulkheadOptions>>();
        var mockOptions2 = new Mock<IOptions<BulkheadOptions>>();
        mockOptions1.Setup(x => x.Value).Returns(options);
        mockOptions2.Setup(x => x.Value).Returns(options);

        var mockLogger1 = new Mock<ILogger<BulkheadPipelineBehavior<SharingTestRequest, TestResponse>>>();
        var mockLogger2 = new Mock<ILogger<BulkheadPipelineBehavior<SharingTestRequest, TestResponse>>>();

        var behavior1 = new BulkheadPipelineBehavior<SharingTestRequest, TestResponse>(mockLogger1.Object, mockOptions1.Object);
        var behavior2 = new BulkheadPipelineBehavior<SharingTestRequest, TestResponse>(mockLogger2.Object, mockOptions2.Object);

        var request = new SharingTestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };

        // Create a delegate that takes time to complete
        var tcs = new TaskCompletionSource<TestResponse>();
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(tcs.Task));

        // Act - Start first request on behavior1
        var firstTask = behavior1.HandleAsync(request, next, CancellationToken.None);

        // Wait a bit to ensure semaphore is acquired
        await Task.Delay(50);

        // Try second request on behavior2 (should be rejected because they share semaphore)
        var secondTask = behavior2.HandleAsync(request, next, CancellationToken.None);

        // Assert - second request should be rejected
        await Assert.ThrowsAsync<BulkheadRejectedException>(() => secondTask.AsTask());

        // Complete the first request
        tcs.SetResult(response);
        var firstResult = await firstTask;
        Assert.Equal(response, firstResult);
    }

    [Fact]
    public async Task HandleAsync_Should_Use_Separate_Semaphores_For_Different_Request_Types()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 1 };
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Create behaviors for different request types
        var mockLogger1 = new Mock<ILogger<BulkheadPipelineBehavior<TestRequest, TestResponse>>>();
        var mockLogger2 = new Mock<ILogger<BulkheadPipelineBehavior<OtherTestRequest, TestResponse>>>();

        var behavior1 = new BulkheadPipelineBehavior<TestRequest, TestResponse>(mockLogger1.Object, _mockOptions.Object);
        var behavior2 = new BulkheadPipelineBehavior<OtherTestRequest, TestResponse>(mockLogger2.Object, _mockOptions.Object);

        var request1 = new TestRequest { Value = "test1" };
        var request2 = new OtherTestRequest { Value = "test2" };
        var response = new TestResponse { Result = "result" };

        // Create delegates that take time to complete
        var tcs1 = new TaskCompletionSource<TestResponse>();
        var tcs2 = new TaskCompletionSource<TestResponse>();
        var next1 = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(tcs1.Task));
        var next2 = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(tcs2.Task));

        // Act - Start requests for both types (should both succeed since different semaphores)
        var task1 = behavior1.HandleAsync(request1, next1, CancellationToken.None);
        var task2 = behavior2.HandleAsync(request2, next2, CancellationToken.None);

        // Complete both requests
        tcs1.SetResult(response);
        tcs2.SetResult(response);

        // Assert - Both should succeed
        var result1 = await task1;
        var result2 = await task2;
        Assert.Equal(response, result1);
        Assert.Equal(response, result2);
    }

    [Fact]
    public async Task HandleAsync_Should_Use_Specific_Request_Type_Concurrency_Limit()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 5, MaxWaitTime = TimeSpan.FromMilliseconds(100) };
        options.SetMaxConcurrency<SpecificConcurrencyTestRequest>(2);
        _mockOptions.Setup(x => x.Value).Returns(options);

        var mockLogger = new Mock<ILogger<BulkheadPipelineBehavior<SpecificConcurrencyTestRequest, TestResponse>>>();
        var behavior = new BulkheadPipelineBehavior<SpecificConcurrencyTestRequest, TestResponse>(mockLogger.Object, _mockOptions.Object);
        var request = new SpecificConcurrencyTestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };

        // Create a delegate that takes time to complete
        var tcs = new TaskCompletionSource<TestResponse>();
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(tcs.Task));

        // Act - Start first two requests (should acquire semaphore)
        var firstTask = behavior.HandleAsync(request, next, CancellationToken.None);
        var secondTask = behavior.HandleAsync(request, next, CancellationToken.None);

        // Wait a bit to ensure semaphores are acquired
        await Task.Delay(50);

        // Try third request (should be rejected)
        var thirdTask = behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert - third request should be rejected
        await Assert.ThrowsAsync<BulkheadRejectedException>(() => thirdTask.AsTask());

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulkhead rejection for SpecificConcurrencyTestRequest: max concurrency (2) exceeded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Complete the first two requests
        tcs.SetResult(response);
        var firstResult = await firstTask;
        var secondResult = await secondTask;
        Assert.Equal(response, firstResult);
        Assert.Equal(response, secondResult);
    }

    [Fact]
    public async Task HandleAsync_Should_Respect_Cancellation_Token()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 1, MaxWaitTime = TimeSpan.FromSeconds(10) };
        _mockOptions.Setup(x => x.Value).Returns(options);
        var mockLogger = new Mock<ILogger<BulkheadPipelineBehavior<CancellationTestRequest, TestResponse>>>();
        var behavior = new BulkheadPipelineBehavior<CancellationTestRequest, TestResponse>(mockLogger.Object, _mockOptions.Object);
        var request = new CancellationTestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };

        // Create a delegate that takes time to complete
        var tcs = new TaskCompletionSource<TestResponse>();
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(tcs.Task));

        // Start first request to hold the semaphore
        var firstTask = behavior.HandleAsync(request, next, CancellationToken.None);

        // Wait for semaphore to be acquired
        await Task.Delay(50);

        // Create cancellation token and cancel it
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - Try second request with cancelled token
        var secondTask = behavior.HandleAsync(request, next, cts.Token);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await secondTask);

        // Complete first request
        tcs.SetResult(response);
        await firstTask;
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_BulkheadRejectedException_When_MaxWaitTime_Expires()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 1, MaxWaitTime = TimeSpan.FromMilliseconds(100) };
        _mockOptions.Setup(x => x.Value).Returns(options);
        var mockLogger = new Mock<ILogger<BulkheadPipelineBehavior<TimeoutTestRequest, TestResponse>>>();
        var behavior = new BulkheadPipelineBehavior<TimeoutTestRequest, TestResponse>(mockLogger.Object, _mockOptions.Object);
        var request = new TimeoutTestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };

        // Create a delegate that takes longer than MaxWaitTime
        var tcs = new TaskCompletionSource<TestResponse>();
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(tcs.Task));

        // Start first request to hold the semaphore
        var firstTask = behavior.HandleAsync(request, next, CancellationToken.None);

        // Wait for semaphore to be acquired
        await Task.Delay(50);

        // Act - Try second request (should timeout and be rejected)
        var secondTask = behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<BulkheadRejectedException>(() => secondTask.AsTask());

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulkhead rejection for TimeoutTestRequest: max concurrency (1) exceeded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Complete first request
        tcs.SetResult(response);
        await firstTask;
    }









    [Fact]
    public void BulkheadOptions_Should_Have_Correct_Defaults()
    {
        // Arrange & Act
        var options = new BulkheadOptions();

        // Assert
        Assert.Equal(Environment.ProcessorCount * 2, options.DefaultMaxConcurrency);
        Assert.Equal(TimeSpan.FromSeconds(5), options.MaxWaitTime);
    }

    [Fact]
    public void BulkheadOptions_SetMaxConcurrency_Generic_Should_Set_Limit_For_Request_Type()
    {
        // Arrange
        var options = new BulkheadOptions();

        // Act
        options.SetMaxConcurrency<TestRequest>(10);

        // Assert
        Assert.Equal(10, options.GetMaxConcurrency(typeof(TestRequest).Name));
    }

    [Fact]
    public void BulkheadOptions_SetMaxConcurrency_By_Name_Should_Set_Limit_For_Request_Type()
    {
        // Arrange
        var options = new BulkheadOptions();

        // Act
        options.SetMaxConcurrency("CustomRequest", 15);

        // Assert
        Assert.Equal(15, options.GetMaxConcurrency("CustomRequest"));
    }

    [Fact]
    public void BulkheadOptions_GetMaxConcurrency_Should_Return_Default_When_Not_Specified()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 8 };

        // Act
        var result = options.GetMaxConcurrency("UnknownRequest");

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public void BulkheadOptions_DisableBulkhead_Should_Set_Concurrency_To_Zero()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 5 };

        // Act
        options.DisableBulkhead<TestRequest>();

        // Assert
        Assert.Equal(0, options.GetMaxConcurrency(typeof(TestRequest).Name));
    }

    [Fact]
    public void BulkheadOptions_Methods_Should_Return_Same_Instance_For_Chaining()
    {
        // Arrange
        var options = new BulkheadOptions();

        // Act
        var result1 = options.SetMaxConcurrency<TestRequest>(10);
        var result2 = options.SetMaxConcurrency("CustomRequest", 15);
        var result3 = options.DisableBulkhead<TestResponse>();

        // Assert
        Assert.Same(options, result1);
        Assert.Same(options, result2);
        Assert.Same(options, result3);
    }

    [Fact]
    public void BulkheadRejectedException_Should_Have_Correct_Properties()
    {
        // Arrange & Act
        var exception = new BulkheadRejectedException("TestRequest", 5);

        // Assert
        Assert.Equal("TestRequest", exception.RequestType);
        Assert.Equal(5, exception.MaxConcurrency);
        Assert.Contains("Bulkhead rejected request type 'TestRequest': maximum concurrency of 5 exceeded", exception.Message);
    }

    // Test helper classes
    public class TestRequest : IRequest<TestResponse>
    {
        public string? Value { get; set; }
    }

    public class OtherTestRequest : IRequest<TestResponse>
    {
        public string? Value { get; set; }
    }

    public class CancellationTestRequest : IRequest<TestResponse>
    {
        public string? Value { get; set; }
    }

    public class RejectionTestRequest : IRequest<TestResponse>
    {
        public string? Value { get; set; }
    }

    public class ExceptionTestRequest : IRequest<TestResponse>
    {
        public string? Value { get; set; }
    }

    public class SharingTestRequest : IRequest<TestResponse>
    {
        public string? Value { get; set; }
    }

    public class TimeoutTestRequest : IRequest<TestResponse>
    {
        public string? Value { get; set; }
    }

    public class SpecificConcurrencyTestRequest : IRequest<TestResponse>
    {
        public string? Value { get; set; }
    }

    public class TestResponse
    {
        public string? Result { get; set; }
    }
}