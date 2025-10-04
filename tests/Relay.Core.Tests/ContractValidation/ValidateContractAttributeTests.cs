using System;
using System.Reflection;
using FluentAssertions;
using Relay.Core.ContractValidation;
using Xunit;

namespace Relay.Core.Tests.ContractValidation
{
    public class ValidateContractAttributeTests
    {
        [Fact]
        public void DefaultConstructor_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var attribute = new ValidateContractAttribute();

            // Assert
            attribute.ValidateRequest.Should().BeTrue();
            attribute.ValidateResponse.Should().BeTrue();
            attribute.ThrowOnValidationFailure.Should().BeTrue();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void ParameterizedConstructor_ShouldSetValuesCorrectly(bool validateRequest, bool validateResponse)
        {
            // Arrange & Act
            var attribute = new ValidateContractAttribute(validateRequest, validateResponse);

            // Assert
            attribute.ValidateRequest.Should().Be(validateRequest);
            attribute.ValidateResponse.Should().Be(validateResponse);
            attribute.ThrowOnValidationFailure.Should().BeTrue(); // Should still be default
        }

        [Fact]
        public void ThrowOnValidationFailure_Property_ShouldBeSettable()
        {
            // Arrange
            var attribute = new ValidateContractAttribute();

            // Act
            attribute.ThrowOnValidationFailure = false;

            // Assert
            attribute.ThrowOnValidationFailure.Should().BeFalse();
        }

        [Fact]
        public void AttributeUsage_ShouldBeCorrect()
        {
            // Arrange
            var attributeType = typeof(ValidateContractAttribute);

            // Act
            var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            usage.Should().NotBeNull();
            usage.ValidOn.Should().Be(AttributeTargets.Class | AttributeTargets.Method);
            usage.AllowMultiple.Should().BeFalse();
            usage.Inherited.Should().BeTrue();
        }

        [ValidateContract(false, false, ThrowOnValidationFailure = false)]
        private class TestClassWithAttribute
        {
            [ValidateContract(true, true, ThrowOnValidationFailure = true)]
            public void TestMethodWithAttribute()
            {
            }
        }

        [Fact]
        public void Attribute_CanBeAppliedToClass()
        {
            // Arrange
            var type = typeof(TestClassWithAttribute);

            // Act
            var attribute = type.GetCustomAttribute<ValidateContractAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute.ValidateRequest.Should().BeFalse();
            attribute.ValidateResponse.Should().BeFalse();
            attribute.ThrowOnValidationFailure.Should().BeFalse();
        }

        [Fact]
        public void Attribute_CanBeAppliedToMethod()
        {            
            // Arrange
            var method = typeof(TestClassWithAttribute).GetMethod(nameof(TestClassWithAttribute.TestMethodWithAttribute));

            // Act
            var attribute = method.GetCustomAttribute<ValidateContractAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute.ValidateRequest.Should().BeTrue();
            attribute.ValidateResponse.Should().BeTrue();
            attribute.ThrowOnValidationFailure.Should().BeTrue();
        }
    }
}
