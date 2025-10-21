using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class FutureValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Future_DateTime()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var futureDateTime = DateTime.Now.AddDays(1);

            // Act
            var errors = await rule.ValidateAsync(futureDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Past_DateTime()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var pastDateTime = DateTime.Now.AddDays(-1);

            // Act
            var errors = await rule.ValidateAsync(pastDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Date must be in the future.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Current_DateTime()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var currentDateTime = DateTime.Now;

            // Act
            var errors = await rule.ValidateAsync(currentDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Date must be in the future.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_DateTime_Just_In_Future()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var futureDateTime = DateTime.Now.AddMilliseconds(1);

            // Act
            var errors = await rule.ValidateAsync(futureDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_DateTime_Just_In_Past()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var pastDateTime = DateTime.Now.AddMilliseconds(-1);

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
            var rule = new FutureValidationRule();
            var minDateTime = DateTime.MinValue;

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
            var rule = new FutureValidationRule();
            var maxDateTime = DateTime.MaxValue;

            // Act
            var errors = await rule.ValidateAsync(maxDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Leap_Year_Date()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var leapYearDate = new DateTime(2024, 2, 29, 12, 0, 0); // Assuming current year is before 2024

            // Act
            var errors = await rule.ValidateAsync(leapYearDate);

            // Assert
            // Depending on current date, but since it's future, should be empty
            if (leapYearDate > DateTime.Now)
            {
                Assert.Empty(errors);
            }
            else
            {
                Assert.Single(errors);
            }
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var futureDateTime = DateTime.Now.AddDays(1);
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
            var rule = new FutureValidationRule();
            var futureDateTime = DateTime.Now.AddDays(1);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(futureDateTime, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Different_Time_Zones()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var utcFuture = DateTime.UtcNow.AddDays(1);

            // Act
            var errors = await rule.ValidateAsync(utcFuture);

            // Assert
            // DateTime.Now considers local time, but since UtcNow +1 day is still future
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_DateTime_With_Ticks()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var now = DateTime.UtcNow;
            // Create a DateTime that is definitely in the future by adding ticks to UTC now
            var preciseFuture = new DateTime(now.Ticks + TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);

            // Act
            var errors = await rule.ValidateAsync(preciseFuture);

            // Assert
            Assert.Empty(errors);
        }
    }
}