using System;
using System.Data.Common;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    public class DefaultTransientErrorDetectorTests
    {
        private class TestDbException : DbException
        {
            public TestDbException(string message) : base(message) { }
        }

        [Fact]
        public void IsTransient_Should_Return_True_For_TimeoutException()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new TimeoutException("Connection timeout");

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }

        [Fact]
        public void IsTransient_Should_Return_True_For_Deadlock_Message()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new InvalidOperationException("Transaction was deadlocked");

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }

        [Fact]
        public void IsTransient_Should_Return_True_For_Connection_Message()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new InvalidOperationException("Connection failed");

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }

        [Fact]
        public void IsTransient_Should_Return_True_For_Network_Message()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new Exception("Network error occurred");

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }

        [Fact]
        public void IsTransient_Should_Return_True_For_Unavailable_Message()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new Exception("Service unavailable");

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }

        [Fact]
        public void IsTransient_Should_Return_True_For_Lock_Message()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new Exception("Lock timeout exceeded");

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }

        [Fact]
        public void IsTransient_Should_Return_True_For_DbException_With_Deadlock()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new TestDbException("Transaction deadlock detected");

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }

        [Fact]
        public void IsTransient_Should_Return_False_For_ArgumentException()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new ArgumentException("Invalid argument");

            var result = detector.IsTransient(exception);

            Assert.False(result);
        }

        [Fact]
        public void IsTransient_Should_Return_False_For_NullReferenceException()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new NullReferenceException("Object reference not set");

            var result = detector.IsTransient(exception);

            Assert.False(result);
        }

        [Fact]
        public void IsTransient_Should_Check_InnerException()
        {
            var detector = new DefaultTransientErrorDetector();
            var innerException = new TimeoutException("Connection timeout");
            var exception = new InvalidOperationException("Operation failed", innerException);

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }

        [Fact]
        public void IsTransient_Should_Return_False_For_Null_Exception()
        {
            var detector = new DefaultTransientErrorDetector();

            var result = detector.IsTransient(null!);

            Assert.False(result);
        }

        [Fact]
        public void IsTransient_Should_Be_Case_Insensitive()
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new Exception("DEADLOCK DETECTED");

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }

        [Theory]
        [InlineData("broken pipe")]
        [InlineData("connection reset")]
        [InlineData("connection refused")]
        [InlineData("host not found")]
        [InlineData("no route to host")]
        [InlineData("transport-level error")]
        [InlineData("communication link failure")]
        public void IsTransient_Should_Detect_Common_Network_Errors(string errorMessage)
        {
            var detector = new DefaultTransientErrorDetector();
            var exception = new Exception(errorMessage);

            var result = detector.IsTransient(exception);

            Assert.True(result);
        }
    }
}
