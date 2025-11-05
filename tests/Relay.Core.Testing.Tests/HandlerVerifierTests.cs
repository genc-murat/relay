using System;
using System.Linq;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

/// <summary>
/// Test data classes for testing handler verifier
/// </summary>
public class TestHandlerRequest : IRequest<string> { }
public class AnotherTestHandlerRequest : IRequest<int> { }

public class HandlerVerifierTests
{
    [Fact]
    public void HandlerVerifier_DefaultState_HasZeroCallCount()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();

        // Assert
        Assert.Equal(0, verifier.CallCount);
        Assert.Empty(verifier.Calls);
    }

    [Fact]
    public void HandlerVerifier_RecordCall_IncreasesCallCount()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var request = new TestHandlerRequest();

        // Act
        verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(verifier, new object[] { request });

        // Assert
        Assert.Equal(1, verifier.CallCount);
        Assert.Single(verifier.Calls);
        Assert.Equal(request, verifier.Calls[0].Request);
        Assert.Equal(1, verifier.Calls[0].SequenceNumber);
        Assert.True(verifier.Calls[0].Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void HandlerVerifier_RecordMultipleCalls_IncreasesCallCountAndMaintainsOrder()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var request1 = new TestHandlerRequest();
        var request2 = new TestHandlerRequest();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Act
        recordMethod.Invoke(verifier, new object[] { request1 });
        recordMethod.Invoke(verifier, new object[] { request2 });

        // Assert
        Assert.Equal(2, verifier.CallCount);
        Assert.Equal(2, verifier.Calls.Count);
        Assert.Equal(request1, verifier.Calls[0].Request);
        Assert.Equal(request2, verifier.Calls[1].Request);
        Assert.Equal(1, verifier.Calls[0].SequenceNumber);
        Assert.Equal(2, verifier.Calls[1].SequenceNumber);
    }

    [Fact]
    public void ShouldHaveBeenCalled_WithCorrectCount_Passes()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act & Assert
        verifier.ShouldHaveBeenCalled(1);
    }

    [Fact]
    public void ShouldHaveBeenCalled_WithIncorrectCount_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            verifier.ShouldHaveBeenCalled(1));
        Assert.Contains("Expected handler to be called 1 time(s), but was called 0 time(s)", exception.Message);
    }

    [Fact]
    public void ShouldHaveBeenCalled_DefaultOverload_WithOneCall_Passes()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act & Assert
        verifier.ShouldHaveBeenCalled();
    }

    [Fact]
    public void ShouldHaveBeenCalled_DefaultOverload_WithNoCalls_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            verifier.ShouldHaveBeenCalled());
        Assert.Contains("Expected handler to be called 1 time(s), but was called 0 time(s)", exception.Message);
    }

    [Fact]
    public void ShouldNotHaveBeenCalled_WithNoCalls_Passes()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();

        // Act & Assert
        verifier.ShouldNotHaveBeenCalled();
    }

    [Fact]
    public void ShouldNotHaveBeenCalled_WithCalls_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            verifier.ShouldNotHaveBeenCalled());
        Assert.Contains("Expected handler to be called 0 time(s), but was called 1 time(s)", exception.Message);
    }

    [Fact]
    public void ShouldHaveBeenCalledWith_Predicate_WithMatchingRequest_Passes()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var request = new TestHandlerRequest();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { request });

        // Act & Assert
        verifier.ShouldHaveBeenCalledWith(r => r == request);
    }

    [Fact]
    public void ShouldHaveBeenCalledWith_Predicate_WithNoMatchingRequest_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            verifier.ShouldHaveBeenCalledWith(r => false));
        Assert.Contains("Expected handler to be called with a request matching the predicate", exception.Message);
    }

    [Fact]
    public void ShouldHaveBeenCalledWith_Request_WithMatchingRequest_Passes()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var request = new TestHandlerRequest();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { request });

        // Act & Assert
        verifier.ShouldHaveBeenCalledWith(request);
    }

    [Fact]
    public void ShouldHaveBeenCalledWith_Request_WithDifferentRequest_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });
        var differentRequest = new TestHandlerRequest();

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            verifier.ShouldHaveBeenCalledWith(differentRequest));
        Assert.Contains("Expected handler to be called with a request matching the predicate", exception.Message);
    }

    [Fact]
    public void ShouldHaveBeenCalledInOrder_WithCorrectOrder_Passes()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var request1 = new TestHandlerRequest();
        var request2 = new TestHandlerRequest();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { request1 });
        recordMethod.Invoke(verifier, new object[] { request2 });

        // Act & Assert
        verifier.ShouldHaveBeenCalledInOrder(request1, request2);
    }

    [Fact]
    public void ShouldHaveBeenCalledInOrder_WithWrongOrder_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var request1 = new TestHandlerRequest();
        var request2 = new TestHandlerRequest();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { request1 });
        recordMethod.Invoke(verifier, new object[] { request2 });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            verifier.ShouldHaveBeenCalledInOrder(request2, request1));
        Assert.Contains("Call 1: Expected request", exception.Message);
    }

    [Fact]
    public void ShouldHaveBeenCalledInOrder_WithWrongCount_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            verifier.ShouldHaveBeenCalledInOrder(new TestHandlerRequest(), new TestHandlerRequest()));
        Assert.Contains("Expected 2 calls, but handler was called 1 time(s)", exception.Message);
    }

    [Fact]
    public void ShouldHaveBeenCalledAtLeast_WithSufficientCalls_Passes()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act & Assert
        verifier.ShouldHaveBeenCalledAtLeast(1);
        verifier.ShouldHaveBeenCalledAtLeast(2);
    }

    [Fact]
    public void ShouldHaveBeenCalledAtLeast_WithInsufficientCalls_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            verifier.ShouldHaveBeenCalledAtLeast(2));
        Assert.Contains("Expected handler to be called at least 2 time(s), but was called 1 time(s)", exception.Message);
    }

    [Fact]
    public void ShouldHaveBeenCalledAtMost_WithSufficientCalls_Passes()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act & Assert
        verifier.ShouldHaveBeenCalledAtMost(1);
        verifier.ShouldHaveBeenCalledAtMost(2);
    }

    [Fact]
    public void ShouldHaveBeenCalledAtMost_WithTooManyCalls_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            verifier.ShouldHaveBeenCalledAtMost(1));
        Assert.Contains("Expected handler to be called at most 1 time(s), but was called 2 time(s)", exception.Message);
    }

    [Fact]
    public void GetRequest_WithValidIndex_ReturnsRequest()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var request1 = new TestHandlerRequest();
        var request2 = new TestHandlerRequest();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { request1 });
        recordMethod.Invoke(verifier, new object[] { request2 });

        // Act & Assert
        Assert.Equal(request1, verifier.GetRequest(0));
        Assert.Equal(request2, verifier.GetRequest(1));
    }

    [Fact]
    public void GetRequest_WithInvalidIndex_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            verifier.GetRequest(0));
        Assert.Contains("Index 0 is out of range", exception.Message);
    }

    [Fact]
    public void GetCallTimestamp_WithValidIndex_ReturnsTimestamp()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var beforeCall = DateTime.UtcNow;
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });
        var afterCall = DateTime.UtcNow;

        // Act
        var timestamp = verifier.GetCallTimestamp(0);

        // Assert
        Assert.True(timestamp >= beforeCall);
        Assert.True(timestamp <= afterCall);
    }

    [Fact]
    public void GetCallTimestamp_WithInvalidIndex_Throws()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            verifier.GetCallTimestamp(0));
        Assert.Contains("Index 0 is out of range", exception.Message);
    }

    [Fact]
    public void Clear_RemovesAllCalls()
    {
        // Arrange
        var verifier = new HandlerVerifier<TestHandlerRequest, string>();
        var recordMethod = verifier.GetType().GetMethod("RecordCall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });
        recordMethod.Invoke(verifier, new object[] { new TestHandlerRequest() });

        // Act
        verifier.Clear();

        // Assert
        Assert.Equal(0, verifier.CallCount);
        Assert.Empty(verifier.Calls);
    }

    [Fact]
    public void HandlerCall_Properties_AreSetCorrectly()
    {
        // Arrange
        var request = new TestHandlerRequest();
        var timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var sequenceNumber = 5;

        // Act
        var call = new HandlerCall<TestHandlerRequest>
        {
            Request = request,
            Timestamp = timestamp,
            SequenceNumber = sequenceNumber
        };

        // Assert
        Assert.Equal(request, call.Request);
        Assert.Equal(timestamp, call.Timestamp);
        Assert.Equal(sequenceNumber, call.SequenceNumber);
    }
}