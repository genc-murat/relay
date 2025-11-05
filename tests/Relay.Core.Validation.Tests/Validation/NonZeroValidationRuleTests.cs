using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class NonZeroValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<int>();
            var request = 42;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Empty_Errors_When_Value_Is_Negative()
        {
            // Arrange
            var rule = new NonZeroValidationRule<int>();
            var request = -42;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<int>();
            var request = 0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Empty_Errors_When_Value_Is_Max_Int()
        {
            // Arrange
            var rule = new NonZeroValidationRule<int>();
            var request = int.MaxValue;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Empty_Errors_When_Value_Is_Min_Int()
        {
            // Arrange
            var rule = new NonZeroValidationRule<int>();
            var request = int.MinValue;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<long>();
            var request = 123456789L;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<long>();
            var request = 0L;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Double_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<double>();
            var request = 3.14159;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Double_Should_Return_Empty_Errors_When_Value_Is_Negative()
        {
            // Arrange
            var rule = new NonZeroValidationRule<double>();
            var request = -2.71828;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Double_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<double>();
            var request = 0.0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Double_Should_Return_Empty_Errors_When_Value_Is_Very_Small()
        {
            // Arrange
            var rule = new NonZeroValidationRule<double>();
            var request = double.Epsilon;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Double_Should_Return_Empty_Errors_When_Value_Is_Negative_Infinity()
        {
            // Arrange
            var rule = new NonZeroValidationRule<double>();
            var request = double.NegativeInfinity;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Double_Should_Return_Empty_Errors_When_Value_Is_Positive_Infinity()
        {
            // Arrange
            var rule = new NonZeroValidationRule<double>();
            var request = double.PositiveInfinity;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Float_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<float>();
            var request = 1.23f;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Float_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<float>();
            var request = 0.0f;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Decimal_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<decimal>();
            var request = 123.45m;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Decimal_Should_Return_Empty_Errors_When_Value_Is_Negative()
        {
            // Arrange
            var rule = new NonZeroValidationRule<decimal>();
            var request = -67.89m;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Decimal_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<decimal>();
            var request = 0.0m;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Short_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<short>();
            var request = (short)42;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Short_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<short>();
            var request = (short)0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Byte_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<byte>();
            var request = (byte)255;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Byte_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<byte>();
            var request = (byte)0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_SByte_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<sbyte>();
            var request = (sbyte)127;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_SByte_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<sbyte>();
            var request = (sbyte)0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_UInt_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<uint>();
            var request = 42u;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_UInt_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<uint>();
            var request = 0u;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_ULong_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<ulong>();
            var request = 18446744073709551615UL; // ulong.MaxValue

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_ULong_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<ulong>();
            var request = 0UL;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_UShort_Should_Return_Empty_Errors_When_Value_Is_Positive()
        {
            // Arrange
            var rule = new NonZeroValidationRule<ushort>();
            var request = (ushort)65535; // ushort.MaxValue

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_UShort_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new NonZeroValidationRule<ushort>();
            var request = (ushort)0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not be zero.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Handle_CancellationToken()
        {
            // Arrange
            var rule = new NonZeroValidationRule<int>();
            var request = 42;
            var cancellationToken = new CancellationToken(true);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                rule.ValidateAsync(request, cancellationToken).AsTask());
        }

        [Fact]
        public async Task ValidateAsync_Double_Should_Handle_NaN_Value()
        {
            // Arrange
            var rule = new NonZeroValidationRule<double>();
            var request = double.NaN;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors); // NaN is not zero, so no error
        }

        [Fact]
        public async Task ValidateAsync_Decimal_Should_Handle_Very_Small_Values()
        {
            // Arrange
            var rule = new NonZeroValidationRule<decimal>();
            var request = 0.0000000000000000000000000001m; // Very small decimal

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Decimal_Should_Handle_Very_Large_Values()
        {
            // Arrange
            var rule = new NonZeroValidationRule<decimal>();
            var request = 79228162514264337593543950335m; // decimal.MaxValue

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }
    }
}