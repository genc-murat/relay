using System;
using Xunit;
using Relay.Core.Validation.Pipeline;

namespace Relay.Core.Tests.Validation
{
    public class ValidationPipelineBehaviorConstructorTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_Validator_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationPipelineBehavior<string, int>(null!));
        }

        [Fact]
        public void StreamConstructor_Should_Throw_ArgumentNullException_When_Validator_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new StreamValidationPipelineBehavior<string, int>(null!));
        }
    }
}