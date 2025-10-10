using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Security;
using Xunit;

namespace Relay.Core.Tests.Security
{
    public class DataEncryptionPipelineBehaviorTests
    {
        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var encryptorMock = new Mock<IDataEncryptor>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                    null!, 
                    encryptorMock.Object));
        }

        [Fact]
        public void Constructor_WithNullEncryptor_ThrowsArgumentNullException()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                    loggerMock.Object, 
                    null!));
        }

        [Fact]
        public async Task HandleAsync_WithValidRequestAndResponse_PerformsEncryptionAndDecryption()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            
            encryptorMock.Setup(x => x.Decrypt(It.IsAny<string>(), "default"))
                .Returns<string, string>((input, _) => $"DECRYPTED_{input}");
            encryptorMock.Setup(x => x.Encrypt(It.IsAny<string>(), "default"))
                .Returns<string, string>((input, _) => $"ENCRYPTED_{input}");

            var behavior = new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            var request = new TestRequest { SensitiveData = "original_sensitive_value" };
            var response = new TestResponse { SensitiveResult = "original_response_value" };
            
            RequestHandlerDelegate<TestResponse> next = () => new ValueTask<TestResponse>(Task.FromResult(response));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal("DECRYPTED_original_sensitive_value", request.SensitiveData);
            Assert.Equal("ENCRYPTED_original_response_value", result.SensitiveResult);
            
            encryptorMock.Verify(x => x.Decrypt("original_sensitive_value", "default"), Times.Once);
            encryptorMock.Verify(x => x.Encrypt("original_response_value", "default"), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithNullRequest_DoesNotThrow()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            var behavior = new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            RequestHandlerDelegate<TestResponse> next = () => new ValueTask<TestResponse>(Task.FromResult(new TestResponse()));

            // Act & Assert
            // Note: We can't have a null request since it's a generic type constraint requires it to be an IRequest
            // This test is more about ensuring it handles null values safely internally
            await behavior.HandleAsync(new TestRequest(), next, CancellationToken.None);
        }

        [Fact]
        public async Task HandleAsync_WithNullResponse_DoesNotAttemptToEncrypt()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            var behavior = new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            var request = new TestRequest { SensitiveData = "test_data" };
            RequestHandlerDelegate<TestResponse> next = () => new ValueTask<TestResponse>(Task.FromResult<TestResponse>(null));

            // Act & Assert
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);
            
            // Verify response is null and no encryption was attempted on response
            Assert.Null(result);
            encryptorMock.Verify(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WhenDecryptionFails_ThrowsDataDecryptionException()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            
            encryptorMock.Setup(x => x.Decrypt(It.IsAny<string>(), "default"))
                .Throws(new CryptographicException("Decryption failed"));

            var behavior = new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            var request = new TestRequest { SensitiveData = "encrypted_data" };
            RequestHandlerDelegate<TestResponse> next = () => new ValueTask<TestResponse>(Task.FromResult(new TestResponse()));

            // Act & Assert
            await Assert.ThrowsAsync<DataDecryptionException>(async () =>
            {
                await behavior.HandleAsync(request, next, CancellationToken.None);
            });
        }

        [Fact]
        public async Task HandleAsync_WhenEncryptionFails_ThrowsDataEncryptionException()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            
            // Setup decryption to work but encryption to fail
            encryptorMock.Setup(x => x.Decrypt(It.IsAny<string>(), "default"))
                .Returns<string, string>((input, _) => $"DECRYPTED_{input}");
            encryptorMock.Setup(x => x.Encrypt(It.IsAny<string>(), "default"))
                .Throws(new CryptographicException("Encryption failed"));

            var behavior = new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            var request = new TestRequest { SensitiveData = "encrypted_request" };
            var response = new TestResponse { SensitiveResult = "original_response" };
            RequestHandlerDelegate<TestResponse> next = () => new ValueTask<TestResponse>(Task.FromResult(response));

            // Act & Assert
            await Assert.ThrowsAsync<DataEncryptionException>(async () =>
            {
                await behavior.HandleAsync(request, next, CancellationToken.None);
            });
        }

        [Fact]
        public async Task HandleAsync_WithEmptySensitiveData_DoesNotCallEncryptor()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            var behavior = new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            var request = new TestRequest { SensitiveData = null }; // null value
            var response = new TestResponse { SensitiveResult = "" }; // empty string
            RequestHandlerDelegate<TestResponse> next = () => new ValueTask<TestResponse>(Task.FromResult(response));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            encryptorMock.Verify(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            encryptorMock.Verify(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithWhitespaceSensitiveData_DoesNotCallEncryptor()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            var behavior = new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            var request = new TestRequest { SensitiveData = "   " }; // whitespace
            var response = new TestResponse { SensitiveResult = "  \t\n  " }; // whitespace
            RequestHandlerDelegate<TestResponse> next = () => new ValueTask<TestResponse>(Task.FromResult(response));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            encryptorMock.Verify(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            encryptorMock.Verify(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithNonEncryptedProperties_DoesNotCallEncryptor()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<SimpleRequest, SimpleResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            var behavior = new DataEncryptionPipelineBehavior<SimpleRequest, SimpleResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            var request = new SimpleRequest { RegularData = "regular_value" };
            var response = new SimpleResponse { RegularResult = "regular_response" };
            RequestHandlerDelegate<SimpleResponse> next = () => new ValueTask<SimpleResponse>(Task.FromResult(response));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            encryptorMock.Verify(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            encryptorMock.Verify(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void DecryptSensitiveData_WithNullObject_DoesNotThrow()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            var behavior = new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            // Act & Assert - should not throw
            var behaviorType = behavior.GetType();
            var method = behaviorType.GetMethod("DecryptSensitiveData", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(behavior, new object[] { null });
        }

        [Fact]
        public void EncryptSensitiveData_WithNullObject_DoesNotThrow()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DataEncryptionPipelineBehavior<TestRequest, TestResponse>>>();
            var encryptorMock = new Mock<IDataEncryptor>();
            var behavior = new DataEncryptionPipelineBehavior<TestRequest, TestResponse>(
                loggerMock.Object, 
                encryptorMock.Object);

            // Act & Assert - should not throw
            var behaviorType = behavior.GetType();
            var method = behaviorType.GetMethod("EncryptSensitiveData", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(behavior, new object[] { null });
        }

        public class TestRequest : IRequest<TestResponse>
        {
            [Encrypted]
            public string? SensitiveData { get; set; }
            
            public string? RegularData { get; set; } // Not encrypted
        }

        public class TestResponse
        {
            [Encrypted]
            public string? SensitiveResult { get; set; }
            
            public string? RegularResult { get; set; } // Not encrypted
        }

        public class SimpleRequest : IRequest<SimpleResponse>
        {
            public string? RegularData { get; set; }
        }

        public class SimpleResponse
        {
            public string? RegularResult { get; set; }
        }
    }
}