using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
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
            Assert.Equal("AES256", attribute.Algorithm);
            Assert.Equal("default", attribute.KeyId);
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
            Assert.Equal("RSA", attribute.Algorithm);
            Assert.Equal("key123", attribute.KeyId);
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
            Assert.NotNull(encryptor);
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
            Assert.NotNull(encrypted);
            Assert.NotEqual(plainText, encrypted);
            Assert.True(encrypted.Length > plainText.Length);
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
            Assert.Equal(plainText, decrypted);
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
            Assert.NotEqual(encrypted2, encrypted1);
        }

        [Fact]
        public void AesDataEncryptor_Encrypt_ShouldHandleNullOrEmpty()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var key = Convert.ToBase64String(new byte[32]);
            var encryptor = new AesDataEncryptor(logger, key);

            // Act & Assert
            Assert.Null(encryptor.Encrypt(null!));
            Assert.Equal("", encryptor.Encrypt(""));
            Assert.Equal(" ", encryptor.Encrypt(" "));
        }

        [Fact]
        public void AesDataEncryptor_Decrypt_ShouldHandleNullOrEmpty()
        {
            // Arrange
            var logger = new Mock<ILogger<AesDataEncryptor>>().Object;
            var key = Convert.ToBase64String(new byte[32]);
            var encryptor = new AesDataEncryptor(logger, key);

            // Act & Assert
            Assert.Null(encryptor.Decrypt(null!));
            Assert.Equal("", encryptor.Decrypt(""));
            Assert.Equal(" ", encryptor.Decrypt(" "));
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
            Assert.Equal(plainText, decrypted);
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
            Assert.Equal(plainText, decrypted);
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
            Assert.Equal(propertyName, exception.PropertyName);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Contains(propertyName, exception.Message);
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
            Assert.Equal(propertyName, exception.PropertyName);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Contains(propertyName, exception.Message);
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
            Assert.NotNull(attribute);
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
            Assert.NotNull(attribute);
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
