using System;
using System.Reflection;
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
            Assert.True(attribute.ValidateRequest);
            Assert.True(attribute.ValidateResponse);
            Assert.True(attribute.ThrowOnValidationFailure);
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
            Assert.Equal(validateRequest, attribute.ValidateRequest);
            Assert.Equal(validateResponse, attribute.ValidateResponse);
            Assert.True(attribute.ThrowOnValidationFailure); // Should still be default
        }

        [Fact]
        public void ThrowOnValidationFailure_Property_ShouldBeSettable()
        {
            // Arrange
            var attribute = new ValidateContractAttribute();

            // Act
            attribute.ThrowOnValidationFailure = false;

            // Assert
            Assert.False(attribute.ThrowOnValidationFailure);
        }

        [Fact]
        public void AttributeUsage_ShouldBeCorrect()
        {
            // Arrange
            var attributeType = typeof(ValidateContractAttribute);

            // Act
            var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(usage);
            Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage.ValidOn);
            Assert.False(usage.AllowMultiple);
            Assert.True(usage.Inherited);
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
            Assert.NotNull(attribute);
            Assert.False(attribute.ValidateRequest);
            Assert.False(attribute.ValidateResponse);
            Assert.False(attribute.ThrowOnValidationFailure);
        }

        [Fact]
        public void Attribute_CanBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestClassWithAttribute).GetMethod(nameof(TestClassWithAttribute.TestMethodWithAttribute));

            // Act
            var attribute = method!.GetCustomAttribute<ValidateContractAttribute>();

            // Assert
            Assert.NotNull(method);
            Assert.NotNull(attribute);
            Assert.True(attribute.ValidateRequest);
            Assert.True(attribute.ValidateResponse);
            Assert.True(attribute.ThrowOnValidationFailure);
        }
    }
}