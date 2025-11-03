using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    /// <summary>
    /// Tests for TransactionOptionsValidator covering all conditional branches.
    /// </summary>
    public class TransactionOptionsValidatorTests
    {
        private readonly TransactionOptionsValidator _validator;

        public TransactionOptionsValidatorTests()
        {
            _validator = new TransactionOptionsValidator();
        }

        [Fact]
        public void Validate_With_Null_Options_Should_Return_Failure()
        {
            // Act
            var result = _validator.Validate("test", null);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("TransactionOptions cannot be null.", result.FailureMessage);
        }

        [Fact]
        public void Validate_With_Negative_Timeout_Not_Infinite_Should_Return_Failure()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultTimeout = TimeSpan.FromMinutes(-5) // Negative but not infinite
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("DefaultTimeout must be non-negative or Timeout.InfiniteTimeSpan", result.FailureMessage);
        }

        [Fact]
        public void Validate_With_Infinite_Timeout_Should_Return_Warning()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultTimeout = System.Threading.Timeout.InfiniteTimeSpan
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert - warnings are treated as failures in ValidateOptionsResult
            Assert.False(result.Succeeded); // Warnings cause the validation to fail
            Assert.Contains("WARNING: DefaultTimeout is set to infinite. This may cause transactions to run indefinitely and block resources. Consider setting a reasonable timeout value (e.g., 30-60 seconds).", result.Failures);
        }

        [Fact]
        public void Validate_With_Zero_Timeout_Should_Return_Warning()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultTimeout = TimeSpan.Zero
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert - warnings are treated as failures in ValidateOptionsResult
            Assert.False(result.Succeeded); // Warnings cause the validation to fail
            Assert.Contains("WARNING: DefaultTimeout is set to infinite. This may cause transactions to run indefinitely and block resources. Consider setting a reasonable timeout value (e.g., 30-60 seconds).", result.Failures);
        }

        [Fact]
        public void Validate_With_Positive_Valid_Timeout_Should_Return_Success()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(45)
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Null(result.FailureMessage);
        }

        [Fact]
        public void Validate_With_Null_RetryPolicy_Should_Return_Success()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultRetryPolicy = null // This is valid
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Validate_With_RetryPolicy_Negative_MaxRetries_Should_Return_Failure()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultRetryPolicy = new TransactionRetryPolicy
                {
                    MaxRetries = -1,
                    InitialDelay = TimeSpan.FromMilliseconds(100)
                }
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("DefaultRetryPolicy.MaxRetries must be non-negative", result.FailureMessage);
        }

        [Fact]
        public void Validate_With_RetryPolicy_Negative_InitialDelay_Should_Return_Failure()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultRetryPolicy = new TransactionRetryPolicy
                {
                    MaxRetries = 3,
                    InitialDelay = TimeSpan.FromMilliseconds(-500) // Negative
                }
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("DefaultRetryPolicy.InitialDelay must be non-negative", result.FailureMessage);
        }

        [Fact]
        public void Validate_With_RetryPolicy_MaxRetries_Over_100_Should_Return_Failure()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultRetryPolicy = new TransactionRetryPolicy
                {
                    MaxRetries = 101, // Over 100
                    InitialDelay = TimeSpan.FromMilliseconds(100)
                }
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("DefaultRetryPolicy.MaxRetries should not exceed 100", result.FailureMessage);
        }

        [Fact]
        public void Validate_With_RetryPolicy_MaxRetries_Greater_Than_10_Should_Return_Warning()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultRetryPolicy = new TransactionRetryPolicy
                {
                    MaxRetries = 15, // Greater than 10 but <= 100
                    InitialDelay = TimeSpan.FromMilliseconds(100)
                }
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert - warnings are treated as failures in ValidateOptionsResult
            Assert.False(result.Succeeded); // Warnings cause the validation to fail
            Assert.Contains("WARNING: DefaultRetryPolicy.MaxRetries is set to 15. High retry counts may cause long delays and resource exhaustion. Consider using a lower value (e.g., 3-5 retries).", result.Failures);
        }

        [Fact]
        public void Validate_With_RetryPolicy_MaxRetries_10_Or_Less_Should_Return_Success()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultRetryPolicy = new TransactionRetryPolicy
                {
                    MaxRetries = 10, // Exactly at the threshold
                    InitialDelay = TimeSpan.FromMilliseconds(100)
                }
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Validate_With_RequireExplicitTransactionAttribute_False_Should_Return_Warning()
        {
            // Arrange
            var options = new TransactionOptions
            {
                RequireExplicitTransactionAttribute = false
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert - warnings are treated as failures in ValidateOptionsResult
            Assert.False(result.Succeeded); // Warnings cause the validation to fail
            Assert.Contains("WARNING: RequireExplicitTransactionAttribute is set to false. This is NOT recommended as it allows implicit transaction behavior. All ITransactionalRequest implementations should have explicit TransactionAttribute with IsolationLevel. Set RequireExplicitTransactionAttribute to true to enforce this requirement.", result.Failures);
        }

        [Fact]
        public void Validate_With_RequireExplicitTransactionAttribute_True_Should_Return_Success()
        {
            // Arrange
            var options = new TransactionOptions
            {
                RequireExplicitTransactionAttribute = true // Default and recommended
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Validate_With_EnableSavepoints_True_And_EnableNestedTransactions_False_Should_Return_Warning()
        {
            // Arrange
            var options = new TransactionOptions
            {
                EnableSavepoints = true,
                EnableNestedTransactions = false
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert - warnings are treated as failures in ValidateOptionsResult
            Assert.False(result.Succeeded); // Warnings cause the validation to fail
            Assert.Contains("WARNING: EnableSavepoints is true but EnableNestedTransactions is false. Savepoints are typically used with nested transactions. Consider enabling nested transactions.", result.Failures);
        }

        [Fact]
        public void Validate_With_EnableSavepoints_True_And_EnableNestedTransactions_True_Should_Return_Success()
        {
            // Arrange
            var options = new TransactionOptions
            {
                EnableSavepoints = true,
                EnableNestedTransactions = true
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Validate_With_EnableSavepoints_False_And_EnableNestedTransactions_False_Should_Return_Success()
        {
            // Arrange
            var options = new TransactionOptions
            {
                EnableSavepoints = false,
                EnableNestedTransactions = false
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Validate_With_All_Valid_Options_Should_Return_Success()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(30),
                RequireExplicitTransactionAttribute = true,
                EnableSavepoints = true,
                EnableNestedTransactions = true,
                DefaultRetryPolicy = new TransactionRetryPolicy
                {
                    MaxRetries = 3,
                    InitialDelay = TimeSpan.FromMilliseconds(100)
                }
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Null(result.FailureMessage);
        }

        [Fact]
        public void Validate_With_Multiple_Validation_Errors_Should_Return_All_Errors()
        {
            // Arrange
            var options = new TransactionOptions
            {
                DefaultTimeout = TimeSpan.FromMinutes(-1), // Invalid negative timeout
                RequireExplicitTransactionAttribute = false, // Warning
                EnableSavepoints = true,
                EnableNestedTransactions = false, // Warning when savepoints enabled
                DefaultRetryPolicy = new TransactionRetryPolicy
                {
                    MaxRetries = -5, // Invalid negative retries
                    InitialDelay = TimeSpan.FromMilliseconds(-500) // Invalid negative delay
                }
            };

            // Act
            var result = _validator.Validate("test", options);

            // Assert
            Assert.False(result.Succeeded);
            // Check for the presence of failure messages (using partial matches to avoid exact formatting issues)
            var failureMessages = string.Join("; ", result.Failures);
            Assert.Contains("DefaultTimeout must be non-negative or Timeout.InfiniteTimeSpan", failureMessages);
            Assert.Contains("DefaultRetryPolicy.MaxRetries must be non-negative", failureMessages);
            Assert.Contains("DefaultRetryPolicy.InitialDelay must be non-negative", failureMessages);
            Assert.Contains("RequireExplicitTransactionAttribute is set to false", failureMessages);
            Assert.Contains("EnableSavepoints is true but EnableNestedTransactions is false", failureMessages);
        }
    }
}