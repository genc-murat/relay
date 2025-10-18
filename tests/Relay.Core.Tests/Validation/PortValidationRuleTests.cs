using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class PortValidationRuleTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(80)]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(49151)]
        [InlineData(65535)]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Port_Is_Valid_And_System_Ports_Allowed(int port)
        {
            // Arrange
            var rule = new PortValidationRule();

            // Act
            var errors = await rule.ValidateAsync(port);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(49151)]
        [InlineData(65535)]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Port_Is_Valid_And_System_Ports_Disallowed(int port)
        {
            // Arrange
            var rule = new PortValidationRule(allowSystemPorts: false);

            // Act
            var errors = await rule.ValidateAsync(port);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(65536)]
        public async Task ValidateAsync_Should_Return_Error_When_Port_Is_Out_Of_Range(int port)
        {
            // Arrange
            var rule = new PortValidationRule();

            // Act
            var errors = await rule.ValidateAsync(port);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Port number must be between 0 and 65535.", errors.First());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(80)]
        [InlineData(1023)]
        public async Task ValidateAsync_Should_Return_Error_When_System_Port_Is_Used_And_Disallowed(int port)
        {
            // Arrange
            var rule = new PortValidationRule(allowSystemPorts: false);

            // Act
            var errors = await rule.ValidateAsync(port);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Port number must be 1024 or greater.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message()
        {
            // Arrange
            var rule = new PortValidationRule(errorMessage: "Custom port error");
            var port = -1;

            // Act
            var errors = await rule.ValidateAsync(port);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom port error", errors.First());
        }
    }
}