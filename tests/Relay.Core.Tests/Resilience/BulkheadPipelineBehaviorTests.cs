using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Resilience;
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
    public async Task HandleAsync_Should_Execute_Successfully_When_Within_Concurrency_Limits()
    {
        // Arrange
        var options = new BulkheadOptions { DefaultMaxConcurrency = 2 };
        _mockOptions.Setup(x => x.Value).Returns(options);
        var behavior = new BulkheadPipelineBehavior<TestRequest, TestResponse>(_mockLogger.Object, _mockOptions.Object);
        var request = new TestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulkhead acquired for TestRequest")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulkhead released for TestRequest")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

    public class TestResponse
    {
        public string? Result { get; set; }
    }
}