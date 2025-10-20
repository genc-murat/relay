using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Options.Retry;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Retry;

namespace Relay.Core.Tests.Retry;

/// <summary>
/// Shared test utilities for RetryPipelineBehavior tests
/// </summary>
public static class RetryPipelineBehaviorTestUtilities
{
    // Test classes
    public class TestRequest { }
    public class TestResponse { }

    [Retry(maxRetryAttempts: 2, retryDelayMilliseconds: 50)]
    public class TestRequestWithAttribute { }

    [Retry(typeof(CustomRetryStrategy), maxRetryAttempts: 2)]
    public class TestRequestWithCustomStrategy { }

    public class CustomRetryStrategy : IRetryStrategy
    {
        public ValueTask<bool> ShouldRetryAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            return new ValueTask<bool>(attempt < 3);
        }

        public ValueTask<TimeSpan> GetRetryDelayAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            return new ValueTask<TimeSpan>(TimeSpan.FromMilliseconds(10));
        }
    }

    /// <summary>
    /// Creates a test behavior with default setup
    /// </summary>
    public static RetryPipelineBehavior<TestRequest, TestResponse> CreateTestBehavior(
        out Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>> loggerMock,
        out Mock<IServiceProvider> serviceProviderMock,
        out RelayOptions relayOptions)
    {
        loggerMock = new Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>>();
        serviceProviderMock = new Mock<IServiceProvider>();
        relayOptions = new RelayOptions
        {
            DefaultRetryOptions = new RetryOptions()
        };

        return new RetryPipelineBehavior<TestRequest, TestResponse>(
            loggerMock.Object,
            Options.Create(relayOptions),
            serviceProviderMock.Object);
    }
}