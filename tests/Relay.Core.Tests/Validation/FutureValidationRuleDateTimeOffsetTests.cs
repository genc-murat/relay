using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class FutureValidationRuleDateTimeOffsetTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Future_DateTimeOffset()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var futureDateTime = DateTimeOffset.Now.AddDays(1);

            // Act
            var errors = await rule.ValidateAsync(futureDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Past_DateTimeOffset()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var pastDateTime = DateTimeOffset.Now.AddDays(-1);

            // Act
            var errors = await rule.ValidateAsync(pastDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Date must be in the future.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Current_DateTimeOffset()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var currentDateTime = DateTimeOffset.Now;

            // Act
            var errors = await rule.ValidateAsync(currentDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Date must be in the future.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_DateTimeOffset_Just_In_Future()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var futureDateTime = DateTimeOffset.Now.AddMilliseconds(1);

            // Act
            var errors = await rule.ValidateAsync(futureDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_DateTimeOffset_Just_In_Past()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var pastDateTime = DateTimeOffset.Now.AddMilliseconds(-1);

            // Act
            var errors = await rule.ValidateAsync(pastDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Date must be in the future.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_MinValue()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var minDateTime = DateTimeOffset.MinValue;

            // Act
            var errors = await rule.ValidateAsync(minDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Date must be in the future.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_MaxValue()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var maxDateTime = DateTimeOffset.MaxValue;

            // Act
            var errors = await rule.ValidateAsync(maxDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Different_Offsets()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var utcFuture = DateTimeOffset.UtcNow.AddDays(1);
            var offsetFuture = new DateTimeOffset(DateTime.UtcNow.AddDays(1), TimeSpan.FromHours(5));

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(utcFuture));
            Assert.Empty(await rule.ValidateAsync(offsetFuture));
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var futureDateTime = DateTimeOffset.Now.AddDays(1);
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync(futureDateTime, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Cancelled_Token()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var futureDateTime = DateTimeOffset.Now.AddDays(1);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(futureDateTime, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Leap_Year_Date()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var leapYearDate = new DateTimeOffset(2024, 2, 29, 12, 0, 0, TimeSpan.Zero);

            // Act
            var errors = await rule.ValidateAsync(leapYearDate);

            // Assert
            if (leapYearDate > DateTimeOffset.Now)
            {
                Assert.Empty(errors);
            }
            else
            {
                Assert.Single(errors);
            }
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_DateTimeOffset_With_Ticks()
        {
            // Arrange
            var rule = new FutureValidationRuleDateTimeOffset();
            var preciseFuture = new DateTimeOffset(DateTimeOffset.Now.Ticks + 1, TimeSpan.Zero);

            // Act
            var errors = await rule.ValidateAsync(preciseFuture);

            // Assert
            Assert.Empty(errors);
        }
    }
}