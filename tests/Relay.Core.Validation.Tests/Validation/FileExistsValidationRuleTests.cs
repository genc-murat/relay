using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class FileExistsValidationRuleTests : IDisposable
    {
        private readonly string _existingFile;
        private readonly string _nonExistingFile;

        public FileExistsValidationRuleTests()
        {
            _existingFile = Path.GetTempFileName();
            _nonExistingFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_File_Exists()
        {
            // Arrange
            var rule = new FileExistsValidationRule();

            // Act
            var errors = await rule.ValidateAsync(_existingFile);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_File_Does_Not_Exist()
        {
            // Arrange
            var rule = new FileExistsValidationRule();

            // Act
            var errors = await rule.ValidateAsync(_nonExistingFile);

            // Assert
            Assert.Single(errors);
            Assert.Equal("File does not exist.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Null_Or_Whitespace_Path()
        {
            // Arrange
            var rule = new FileExistsValidationRule();

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
            var rule = new FileExistsValidationRule("Custom error");

            // Act
            var errors = await rule.ValidateAsync(_nonExistingFile);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom error", errors.First());
        }

        public void Dispose()
        {
            if (File.Exists(_existingFile))
            {
                File.Delete(_existingFile);
            }
        }
    }
}