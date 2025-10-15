using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class GuidValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_GUID_Is_Valid()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = Guid.NewGuid().ToString();

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_GUID_Is_Null()
        {
            // Arrange
            var rule = new GuidValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_GUID_Is_Invalid()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = "not-a-guid";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid GUID format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new GuidValidationRule("Custom GUID error");
            var request = "invalid-guid";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom GUID error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Accept_GUID_Without_Hyphens()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var guid = Guid.NewGuid();
            var request = guid.ToString("N"); // No hyphens

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Accept_GUID_With_Hyphens()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var guid = Guid.NewGuid();
            var request = guid.ToString("D"); // With hyphens

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync(request, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Cancelled_Token()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = "invalid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}