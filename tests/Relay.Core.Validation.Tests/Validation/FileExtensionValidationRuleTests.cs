using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class FileExtensionValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Extension_Is_Allowed()
        {
            // Arrange
            var rule = new FileExtensionValidationRule(new[] { "txt", "pdf" });
            var request = "document.txt";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Extension_Is_Not_Allowed()
        {
            // Arrange
            var rule = new FileExtensionValidationRule(new[] { "txt", "pdf" });
            var request = "document.exe";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("File extension must be one of: txt, pdf.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Be_Case_Insensitive()
        {
            // Arrange
            var rule = new FileExtensionValidationRule(new[] { "TXT" });
            var request = "document.txt";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Extensions_With_Dots()
        {
            // Arrange
            var rule = new FileExtensionValidationRule(new[] { ".txt" });
            var request = "document.txt";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_File_Has_No_Extension()
        {
            // Arrange
            var rule = new FileExtensionValidationRule(new[] { "txt" });
            var request = "document";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("File extension must be one of: txt.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new FileExtensionValidationRule(new[] { "txt" }, "Custom file extension error");
            var request = "document.pdf";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom file extension error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_File_Path_Is_Null()
        {
            // Arrange
            var rule = new FileExtensionValidationRule(new[] { "txt" });
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new FileExtensionValidationRule(new[] { "txt" });
            var request = "document.txt";
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
            var rule = new FileExtensionValidationRule(new[] { "txt" });
            var request = "document.pdf";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}