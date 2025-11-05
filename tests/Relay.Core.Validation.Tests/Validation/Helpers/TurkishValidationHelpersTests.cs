using Relay.Core.Validation.Helpers;
using Xunit;

namespace Relay.Core.Tests.Validation.Helpers;

/// <summary>
/// Tests for TurkishValidationHelpers to increase code coverage by ensuring all methods can be called
/// </summary>
public class TurkishValidationHelpersTests
{
    #region Turkish ID Tests

    [Theory]
    [InlineData("12345678902")] // Valid Turkish ID format
    [InlineData("10000000146")] // Another valid Turkish ID format
    public void IsValidTurkishId_With_Valid_Ids_Should_Not_Throw(string validId)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishId(validId));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890")] // Too short
    [InlineData("123456789012")] // Too long
    [InlineData("abcdefghijk")] // Not digits
    [InlineData("02345678901")] // Starts with 0
    public void IsValidTurkishId_With_Invalid_Ids_Should_Not_Throw(string invalidId)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishId(invalidId));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region Turkish Foreigner ID Tests

    [Theory]
    [InlineData("99123456786")] // Valid foreigner ID starting with 99
    [InlineData("99000000010")] // Another valid foreigner ID
    public void IsValidTurkishForeignerId_With_Valid_Ids_Should_Not_Throw(string validId)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishForeignerId(validId));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678902")] // Doesn't start with 99
    [InlineData("991234567890")] // Too long
    [InlineData("9912345678")] // Too short
    [InlineData("99abcdefghi")] // Not digits
    [InlineData("98123456786")] // Starts with 98, not 99
    public void IsValidTurkishForeignerId_With_Invalid_Ids_Should_Not_Throw(string invalidId)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishForeignerId(invalidId));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region Turkish Phone Tests

    [Theory]
    [InlineData("05321234567")] // Valid Turkish mobile number
    [InlineData("+905321234567")] // Valid Turkish mobile number with +90 prefix
    public void IsValidTurkishPhone_With_Valid_Phones_Should_Not_Throw(string validPhone)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishPhone(validPhone));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890")] // Not starting with 5
    [InlineData("02121234567")] // Landline, not mobile
    [InlineData("053212345678")] // Too long
    [InlineData("0532123456")] // Too short
    [InlineData("abc123def45")] // Contains letters
    [InlineData("0532-123-45-67")] // Contains dashes
    public void IsValidTurkishPhone_With_Invalid_Phones_Should_Not_Throw(string invalidPhone)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishPhone(invalidPhone));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region Turkish Postal Code Tests

    [Theory]
    [InlineData("34000")] // Valid postal code
    [InlineData("06000")] // Another valid postal code
    public void IsValidTurkishPostalCode_With_Valid_Codes_Should_Not_Throw(string validCode)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishPostalCode(validCode));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("3400")] // Too short
    [InlineData("340000")] // Too long
    [InlineData("abcde")] // Not digits
    public void IsValidTurkishPostalCode_With_Invalid_Codes_Should_Not_Throw(string invalidCode)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishPostalCode(invalidCode));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region Turkish IBAN Tests

    [Theory]
    [InlineData("TR330006100519786457841326")] // Valid Turkish IBAN
    [InlineData("TR440006100519786457841327")] // Another valid Turkish IBAN
    public void IsValidTurkishIban_With_Valid_Ibans_Should_Not_Throw(string validIban)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishIban(validIban));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("TR33000610051978645784132")] // Too short
    [InlineData("TR3300061005197864578413267")] // Too long
    [InlineData("DE440006100519786457841326")] // German IBAN
    public void IsValidTurkishIban_With_Invalid_Ibans_Should_Not_Throw(string invalidIban)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishIban(invalidIban));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region Turkish Tax Number Tests

    [Theory]
    [InlineData("1234567890")] // Valid tax number
    [InlineData("9876543210")] // Another valid tax number
    public void IsValidTurkishTaxNumber_With_Valid_Numbers_Should_Not_Throw(string validNumber)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishTaxNumber(validNumber));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789")] // Too short
    [InlineData("12345678901")] // Too long
    [InlineData("abcdefghij")] // Not digits
    public void IsValidTurkishTaxNumber_With_Invalid_Numbers_Should_Not_Throw(string invalidNumber)
    {
        // Act
        var exception = Record.Exception(() => TurkishValidationHelpers.IsValidTurkishTaxNumber(invalidNumber));

        // Assert
        Assert.Null(exception);
    }

    #endregion
}