using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class DirectoryExistsValidationRuleTests : IDisposable
    {
        private readonly string _existingDirectory;
        private readonly string _nonExistingDirectory;

        public DirectoryExistsValidationRuleTests()
        {
            _existingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_existingDirectory);
            _nonExistingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Directory_Exists()
        {
            // Arrange
            var rule = new DirectoryExistsValidationRule();

            // Act
            var errors = await rule.ValidateAsync(_existingDirectory);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Directory_Does_Not_Exist()
        {
            // Arrange
            var rule = new DirectoryExistsValidationRule();

            // Act
            var errors = await rule.ValidateAsync(_nonExistingDirectory);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Directory does not exist.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Null_Or_Whitespace_Path()
        {
            // Arrange
            var rule = new DirectoryExistsValidationRule();

            // Act
            var errorsNull = await rule.ValidateAsync(null);
            var errorsEmpty = await rule.ValidateAsync("");
            var errorsWhitespace = await rule.ValidateAsync("   ");

            // Assert
            Assert.Single(errorsNull);
            Assert.Equal("Path cannot be null or whitespace.", errorsNull.First());
            Assert.Single(errorsEmpty);
            Assert.Equal("Path cannot be null or whitespace.", errorsEmpty.First());
            Assert.Single(errorsWhitespace);
            Assert.Equal("Path cannot be null or whitespace.", errorsWhitespace.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message()
        {
            // Arrange
            var rule = new DirectoryExistsValidationRule("Custom error");

            // Act
            var errors = await rule.ValidateAsync(_nonExistingDirectory);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom error", errors.First());
        }

        public void Dispose()
        {
            if (Directory.Exists(_existingDirectory))
            {
                Directory.Delete(_existingDirectory);
            }
        }
    }
}