using Moq;
using Relay.Core.Validation.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class UniqueUsernameValidationRuleEnhancedTests
{
    [Fact]
    public async Task ValidateAsync_With_Cancelled_CancellationToken_Throws_OperationCanceledException()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await rule.ValidateAsync("user", cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_With_Special_Characters_In_Username()
    {
        // Arrange
        var specialCharUsernames = new[]
        {
            "user@domain.com",
            "user_name-test",
            "user.name_test",
            "user+tag@example.com",
            "user#tag",
            "user$tag",
            "user%tag",
            "user^tag",
            "user&tag",
            "user*tag"
        };

        foreach (var username in specialCharUsernames)
        {
            var mockChecker = new Mock<IUsernameUniquenessChecker>();
            mockChecker
                .Setup(x => x.IsUsernameUniqueAsync(username, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var rule = new UniqueUsernameValidationRule(mockChecker.Object);

            // Act
            var result = await rule.ValidateAsync(username);

            // Assert
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task ValidateAsync_With_Unicode_Usernames()
    {
        // Arrange
        var unicodeUsernames = new[]
        {
            "用户", // Chinese characters
            "사용자", // Korean characters
            "χρήστης", // Greek
            "उपयोगकर्ता", // Hindi
            "пользователь", // Russian
            "usuario", // Spanish
            "utilisateur", // French
            "benutzer", // German
            "مستخدم", // Arabic
            "ユーザー" // Japanese
        };

        foreach (var username in unicodeUsernames)
        {
            var mockChecker = new Mock<IUsernameUniquenessChecker>();
            mockChecker
                .Setup(x => x.IsUsernameUniqueAsync(username, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var rule = new UniqueUsernameValidationRule(mockChecker.Object);

            // Act
            var result = await rule.ValidateAsync(username);

            // Assert
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task ValidateAsync_With_Very_Long_Username_Handles_Properly()
    {
        // Arrange
        var veryLongUsername = new string('a', 10000); // Extremely long username
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync(veryLongUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync(veryLongUsername);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Username_At_Exact_Boundary_Conditions()
    {
        // Arrange
        var boundaryUsernames = new[]
        {
            "a", // Minimum length 1
            new string('x', 254), // Near maximum typical length
            new string('y', 255), // Maximum typical length in many systems
        };

        foreach (var username in boundaryUsernames)
        {
            var mockChecker = new Mock<IUsernameUniquenessChecker>();
            mockChecker
                .Setup(x => x.IsUsernameUniqueAsync(username, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var rule = new UniqueUsernameValidationRule(mockChecker.Object);

            // Act
            var result = await rule.ValidateAsync(username);

            // Assert
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task ValidateAsync_With_TaskCanceledException_Handled_Gracefully()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("user", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Operation was cancelled due to timeout"));

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("user");

        // Assert
        Assert.Single(result);
        Assert.Equal("Unable to verify username uniqueness. Please try again later.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_With_TimeoutException_Handled_Gracefully()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("user", It.IsAny<CancellationToken>()))
            .Throws(new TimeoutException("Database query timeout"));

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("user");

        // Assert
        Assert.Single(result);
        Assert.Equal("Unable to verify username uniqueness. Please try again later.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_With_NetworkException_Handled_Gracefully()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("user", It.IsAny<CancellationToken>()))
            .Throws(new System.Net.WebException("Network connectivity issue"));

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("user");

        // Assert
        Assert.Single(result);
        Assert.Equal("Unable to verify username uniqueness. Please try again later.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_With_DatabaseException_Handled_Gracefully()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("user", It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Database connection failed"));

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("user");

        // Assert
        Assert.Single(result);
        Assert.Equal("Unable to verify username uniqueness. Please try again later.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_With_Multiple_Concurrent_Calls_To_Same_Username()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        var callCount = 0;
        
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return true;
            });

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act - Make multiple concurrent calls
        var tasks = new List<Task<IEnumerable<string>>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(rule.ValidateAsync("user").AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        foreach (var result in results)
        {
            Assert.Empty(result);
        }
        Assert.Equal(10, callCount); // Each call should result in a uniqueness check
    }

    [Fact]
    public async Task ValidateAsync_With_Empty_String_After_Trimming_Handled_Correctly()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        // The checker should not be called for whitespace-only usernames
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("   "); // Spaces only

        // Assert
        Assert.Empty(result);
        // Verify that the uniqueness checker was NOT called
        mockChecker.Verify(x => x.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_With_Different_Whitespace_Combinations()
    {
        // Arrange
        var whitespaceVariants = new[]
        {
            "", // Empty string
            " ", // Single space
            "  ", // Two spaces
            "\t", // Tab
            "\n", // Newline
            "\r", // Carriage return
            "\r\n", // CRLF
            " \t \n ", // Mixed whitespace
            "\u00A0", // Non-breaking space
            "\u2000", // En quad
            "\u2001", // Em quad
            "\u2002", // En space
            "\u2003", // Em space
            "\u2004", // Three-per-em space
            "\u2005", // Four-per-em space
            "\u2006", // Six-per-em space
            "\u2007", // Figure space
            "\u2008", // Punctuation space
            "\u2009", // Thin space
            "\u200A", // Hair space
            "\u2028", // Line separator
            "\u2029", // Paragraph separator
            "\u202F", // Narrow no-break space
            "\u205F", // Medium mathematical space
            "\u3000"  // Ideographic space
        };

        foreach (var username in whitespaceVariants)
        {
            var mockChecker = new Mock<IUsernameUniquenessChecker>();
            // The checker should not be called for whitespace-only usernames
            mockChecker
                .Setup(x => x.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var rule = new UniqueUsernameValidationRule(mockChecker.Object);

            // Act
            var result = await rule.ValidateAsync(username);

            // Assert
            Assert.Empty(result);
            // Verify that the uniqueness checker was NOT called
            mockChecker.Verify(x => x.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task ValidateAsync_With_Mixed_Case_Usernames()
    {
        // Arrange
        var mixedCaseUsernames = new[]
        {
            "User",
            "USER",
            "user",
            "UsEr",
            "uSeR",
            "USERNAME",
            "username",
            "UserName",
            "User_Name",
            "User.Name"
        };

        foreach (var username in mixedCaseUsernames)
        {
            var mockChecker = new Mock<IUsernameUniquenessChecker>();
            mockChecker
                .Setup(x => x.IsUsernameUniqueAsync(username, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var rule = new UniqueUsernameValidationRule(mockChecker.Object);

            // Act
            var result = await rule.ValidateAsync(username);

            // Assert
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task ValidateAsync_With_Numeric_Usernames()
    {
        // Arrange
        var numericUsernames = new[]
        {
            "123",
            "1234567890",
            "0000",
            "9999",
            "12345",
            "abc123",
            "123abc",
            "a1b2c3",
            "user123",
            "123user"
        };

        foreach (var username in numericUsernames)
        {
            var mockChecker = new Mock<IUsernameUniquenessChecker>();
            mockChecker
                .Setup(x => x.IsUsernameUniqueAsync(username, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var rule = new UniqueUsernameValidationRule(mockChecker.Object);

            // Act
            var result = await rule.ValidateAsync(username);

            // Assert
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task ValidateAsync_With_Extreme_Edge_Cases()
    {
        // Arrange
        var edgeCaseUsernames = new[]
        {
            ".", // Single period
            "..", // Double period
            "...", // Triple period
            "-", // Single hyphen
            "--", // Double hyphen
            "_", // Single underscore
            "__", // Double underscore
            "@", // Single at sign
            "#", // Hash
            "$", // Dollar sign
            "%", // Percent
            "^", // Caret
            "&", // Ampersand
            "*", // Asterisk
            "()", // Parentheses
            "[]", // Square brackets
            "{}", // Curly braces
            "<>", // Angle brackets
            "|", // Pipe
            "\\", // Backslash
            "/", // Forward slash
            "?", // Question mark
            "!", // Exclamation mark
            "~", // Tilde
            "`", // Backtick
            "'", // Single quote
            "\"", // Double quote
            ";", // Semicolon
            ":", // Colon
            ",", // Comma
            "." // Period
        };

        foreach (var username in edgeCaseUsernames)
        {
            var mockChecker = new Mock<IUsernameUniquenessChecker>();
            mockChecker
                .Setup(x => x.IsUsernameUniqueAsync(username, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var rule = new UniqueUsernameValidationRule(mockChecker.Object);

            // Act
            var result = await rule.ValidateAsync(username);

            // Assert
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task ValidateAsync_With_Null_CancellationToken_Handled_Properly()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("user", CancellationToken.None))
            .ReturnsAsync(true);

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("user", CancellationToken.None);

        // Assert
        Assert.Empty(result);
        mockChecker.Verify(x => x.IsUsernameUniqueAsync("user", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_With_PreCancelled_Token_Does_Not_Call_Checker()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await rule.ValidateAsync("user", cts.Token));

        // Verify that the uniqueness checker was NOT called
        mockChecker.Verify(x => x.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}