using Relay.Core.Configuration.Core;
using Relay.Core.Configuration.Options;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Configuration
{
    public class RelayOptionsValidatorTests
    {
        private readonly RelayOptionsValidator _validator = new();

        [Fact]
        public void Validate_WithNullOptions_ReturnsFailure()
        {
            // Act
            var result = _validator.Validate(null, null!);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("RelayOptions cannot be null.", result.Failures);
        }

        [Fact]
        public void Validate_WithValidOptions_ReturnsSuccess()
        {
            // Arrange
            var options = new RelayOptions();

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Failed);
        }

        [Fact]
        public void Validate_WithInvalidMaxConcurrentNotificationHandlers_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions
            {
                MaxConcurrentNotificationHandlers = 0
            };

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("MaxConcurrentNotificationHandlers must be greater than 0.", result.Failures);
        }

        [Fact]
        public void Validate_WithNullHandlerOptions_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions
            {
                DefaultHandlerOptions = null!
            };

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultHandlerOptions cannot be null.", result.Failures);
        }

        [Fact]
        public void Validate_WithInvalidHandlerTimeout_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultHandlerOptions.DefaultTimeout = TimeSpan.FromSeconds(-1);

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultHandlerOptions.DefaultTimeout must be greater than zero when specified.", result.Failures);
        }

        [Fact]
        public void Validate_WithNegativeMaxRetryAttempts_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultHandlerOptions.MaxRetryAttempts = -1;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultHandlerOptions.MaxRetryAttempts cannot be negative.", result.Failures);
        }

        [Fact]
        public void Validate_WithRetryEnabledButZeroAttempts_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultHandlerOptions.EnableRetry = true;
            options.DefaultHandlerOptions.MaxRetryAttempts = 0;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultHandlerOptions.MaxRetryAttempts must be greater than 0 when EnableRetry is true.", result.Failures);
        }

        [Fact]
        public void Validate_WithInvalidNotificationDispatchMode_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultNotificationOptions.DefaultDispatchMode = (NotificationDispatchMode)999;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultNotificationOptions.DefaultDispatchMode has an invalid value.", result.Failures);
        }

        [Fact]
        public void Validate_WithInvalidNotificationTimeout_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultNotificationOptions.DefaultTimeout = TimeSpan.Zero;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultNotificationOptions.DefaultTimeout must be greater than zero when specified.", result.Failures);
        }

        [Fact]
        public void Validate_WithInvalidMaxDegreeOfParallelism_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultNotificationOptions.MaxDegreeOfParallelism = 0;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultNotificationOptions.MaxDegreeOfParallelism must be greater than 0.", result.Failures);
        }

        [Fact]
        public void Validate_WithInvalidPipelineScope_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultPipelineOptions.DefaultScope = (PipelineScope)999;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultPipelineOptions.DefaultScope has an invalid value.", result.Failures);
        }

        [Fact]
        public void Validate_WithEmptyHttpMethod_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultEndpointOptions.DefaultHttpMethod = "";

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultEndpointOptions.DefaultHttpMethod cannot be null or empty.", result.Failures);
        }

        [Fact]
        public void Validate_WithInvalidHttpMethod_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultEndpointOptions.DefaultHttpMethod = "INVALID";

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultEndpointOptions.DefaultHttpMethod must be a valid HTTP method.", result.Failures);
        }

        [Fact]
        public void Validate_WithConsecutiveSlashesInRoutePrefix_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultEndpointOptions.DefaultRoutePrefix = "api//v1";

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultEndpointOptions.DefaultRoutePrefix cannot contain consecutive slashes.", result.Failures);
        }

        [Fact]
        public void Validate_WithLeadingSlashInRoutePrefix_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.DefaultEndpointOptions.DefaultRoutePrefix = "/api/v1";

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultEndpointOptions.DefaultRoutePrefix should not start with a slash unless it's the root path.", result.Failures);
        }

        [Fact]
        public void Validate_WithEmptyHandlerOverrideKey_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.HandlerOverrides[""] = new HandlerOptions();

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("Handler override keys cannot be null or empty.", result.Failures);
        }

        [Fact]
        public void Validate_WithInvalidHandlerOverride_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.HandlerOverrides["TestHandler.Handle"] = new HandlerOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(-5)
            };

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("HandlerOverrides[TestHandler.Handle].DefaultTimeout must be greater than zero when specified.", result.Failures);
        }

        [Fact]
        public void Validate_WithNullNotificationOptions_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions
            {
                DefaultNotificationOptions = null!
            };

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultNotificationOptions cannot be null.", result.Failures);
        }

        [Fact]
        public void Validate_WithNullPipelineOptions_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions
            {
                DefaultPipelineOptions = null!
            };

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultPipelineOptions cannot be null.", result.Failures);
        }

        [Fact]
        public void Validate_WithNullEndpointOptions_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions
            {
                DefaultEndpointOptions = null!
            };

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("DefaultEndpointOptions cannot be null.", result.Failures);
        }

        [Fact]
        public void Validate_WithEmptyNotificationOverrideKey_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.NotificationOverrides[""] = new NotificationOptions();

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("Notification override keys cannot be null or empty.", result.Failures);
        }

        [Fact]
        public void Validate_WithInvalidNotificationOverride_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.NotificationOverrides["TestNotification.Handle"] = new NotificationOptions
            {
                MaxDegreeOfParallelism = 0
            };

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("NotificationOverrides[TestNotification.Handle].MaxDegreeOfParallelism must be greater than 0.", result.Failures);
        }

        [Fact]
        public void Validate_WithEmptyPipelineOverrideKey_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.PipelineOverrides[""] = new PipelineOptions();

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("Pipeline override keys cannot be null or empty.", result.Failures);
        }

        [Fact]
        public void Validate_WithInvalidPipelineOverride_ReturnsFailure()
        {
            // Arrange
            var options = new RelayOptions();
            options.PipelineOverrides["TestPipeline.Execute"] = new PipelineOptions
            {
                DefaultScope = (PipelineScope)999
            };

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains("PipelineOverrides[TestPipeline.Execute].DefaultScope has an invalid value.", result.Failures);
        }

        [Fact]
        public void Validate_WithMultipleFailures_ReturnsAllFailures()
        {
            // Arrange
            var options = new RelayOptions
            {
                MaxConcurrentNotificationHandlers = 0
            };
            options.DefaultHandlerOptions.MaxRetryAttempts = -1;
            options.DefaultEndpointOptions.DefaultHttpMethod = "";

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.True(result.Failures.Count() >= 3);
        }
    }
}