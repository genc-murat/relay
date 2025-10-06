using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Requests;
using Relay.Core.Security;
using Xunit;

namespace Relay.Core.Tests.Security
{
    public class DataEncryptionTests
    {
        [Fact]
        public void EncryptedAttribute_ShouldHaveDefaultValues()
        {
            // Act
            var attribute = new EncryptedAttribute();

            // Assert
            attribute.Algorithm.Should().Be("AES256");
            attribute.KeyId.Should().Be("default");
        }

        [Fact]
        public void EncryptedAttribute_ShouldAllowCustomValues()
        {
            // Act
            var attribute = new EncryptedAttribute
            {
                Algorithm = "RSA",
                KeyId = "key123"
            };

            // Assert
            attribute.Algorithm.Should().Be("RSA");
            attribute.KeyId.Should().Be("key123");
        }

        [Fact]
        public void AesDataEncryptor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange
            var key = Convert.ToBase64String(new byte[32]);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AesDataEncryptor(null!, key));
        }

        [Fact]
        public void AesDataEncryptor_ShouldThrowArgumentNullException_WhenKeyIsNull()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AesDataEncryptor(logger, null!));
        }

        [Fact]
        public void AesDataEncryptor_ShouldThrowArgumentException_WhenKeyIsInvalidLength()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var invalidKey = Convert.ToBase64String(new byte[16]); // Too short

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new AesDataEncryptor(logger, invalidKey));
        }

        [Fact]
        public void AesDataEncryptor_ShouldInitialize_WithValidKey()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var validKey = Convert.ToBase64String(new byte[32]); // 256 bits

            // Act
            var encryptor = new AesDataEncryptor(logger, validKey);

            // Assert
            encryptor.Should().NotBeNull();
        }

        [Fact]
        public void AesDataEncryptor_Encrypt_ShouldReturnEncryptedString()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var key = Convert.ToBase64String(new byte[32]);
            var encryptor = new AesDataEncryptor(logger, key);
            var plainText = "Hello, World!";

            // Act
            var encrypted = encryptor.Encrypt(plainText);

            // Assert
            encrypted.Should().NotBeNull();
            encrypted.Should().NotBe(plainText);
            encrypted.Length.Should().BeGreaterThan(plainText.Length);
        }

        [Fact]
        public void AesDataEncryptor_Decrypt_ShouldReturnOriginalString()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var key = Convert.ToBase64String(new byte[32]);
            var encryptor = new AesDataEncryptor(logger, key);
            var plainText = "Hello, World!";

            // Act
            var encrypted = encryptor.Encrypt(plainText);
            var decrypted = encryptor.Decrypt(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public void AesDataEncryptor_Encrypt_ShouldReturnDifferentResultEachTime()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var key = Convert.ToBase64String(new byte[32]);
            var encryptor = new AesDataEncryptor(logger, key);
            var plainText = "Hello, World!";

            // Act
            var encrypted1 = encryptor.Encrypt(plainText);
            var encrypted2 = encryptor.Encrypt(plainText);

            // Assert - Should be different due to random IV
            encrypted1.Should().NotBe(encrypted2);
        }

        [Fact]
        public void AesDataEncryptor_Encrypt_ShouldHandleNullOrEmpty()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var key = Convert.ToBase64String(new byte[32]);
            var encryptor = new AesDataEncryptor(logger, key);

            // Act & Assert
            encryptor.Encrypt(null!).Should().BeNull();
            encryptor.Encrypt("").Should().Be("");
            encryptor.Encrypt(" ").Should().Be(" ");
        }

        [Fact]
        public void AesDataEncryptor_Decrypt_ShouldHandleNullOrEmpty()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var key = Convert.ToBase64String(new byte[32]);
            var encryptor = new AesDataEncryptor(logger, key);

            // Act & Assert
            encryptor.Decrypt(null!).Should().BeNull();
            encryptor.Decrypt("").Should().Be("");
            encryptor.Decrypt(" ").Should().Be(" ");
        }

        [Fact]
        public void AesDataEncryptor_ShouldHandleUnicodeCharacters()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var key = Convert.ToBase64String(new byte[32]);
            var encryptor = new AesDataEncryptor(logger, key);
            var plainText = "Türkçe karakterler: ğüşıöçĞÜŞİÖÇ 你好世界";

            // Act
            var encrypted = encryptor.Encrypt(plainText);
            var decrypted = encryptor.Decrypt(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public void AesDataEncryptor_ShouldHandleLargeStrings()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var key = Convert.ToBase64String(new byte[32]);
            var encryptor = new AesDataEncryptor(logger, key);
            var plainText = new string('A', 10000);

            // Act
            var encrypted = encryptor.Encrypt(plainText);
            var decrypted = encryptor.Decrypt(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public void DataEncryptionException_ShouldContainPropertyName()
        {
            // Arrange
            var innerException = new Exception("Inner error");
            var propertyName = "Password";

            // Act
            var exception = new DataEncryptionException(propertyName, innerException);

            // Assert
            exception.PropertyName.Should().Be(propertyName);
            exception.InnerException.Should().Be(innerException);
            exception.Message.Should().Contain(propertyName);
        }

        [Fact]
        public void DataDecryptionException_ShouldContainPropertyName()
        {
            // Arrange
            var innerException = new Exception("Inner error");
            var propertyName = "CreditCard";

            // Act
            var exception = new DataDecryptionException(propertyName, innerException);

            // Assert
            exception.PropertyName.Should().Be(propertyName);
            exception.InnerException.Should().Be(innerException);
            exception.Message.Should().Contain(propertyName);
        }

        [Fact]
        public void TestRequest_WithEncryptedAttribute_ShouldBeMarkedCorrectly()
        {
            // Arrange
            var request = new TestRequest { SensitiveData = "secret123" };

            // Act
            var property = typeof(TestRequest).GetProperty(nameof(TestRequest.SensitiveData));
            var attribute = property?.GetCustomAttribute<EncryptedAttribute>();

            // Assert
            attribute.Should().NotBeNull();
        }

        [Fact]
        public void TestResponse_WithEncryptedAttribute_ShouldBeMarkedCorrectly()
        {
            // Arrange
            var response = new TestResponse { SensitiveResult = "result456" };

            // Act
            var property = typeof(TestResponse).GetProperty(nameof(TestResponse.SensitiveResult));
            var attribute = property?.GetCustomAttribute<EncryptedAttribute>();

            // Assert
            attribute.Should().NotBeNull();
        }

        public class TestRequest : IRequest<TestResponse>
        {
            [Encrypted]
            public string? SensitiveData { get; set; }
        }

        public class TestResponse
        {
            [Encrypted]
            public string? SensitiveResult { get; set; }
        }
    }
}
