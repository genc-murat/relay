using System;
using System.Linq;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing;

/// <summary>
/// TestRelay-specific assertion helpers.
/// </summary>
public static class TestRelayAssertions
{
    /// <summary>
    /// Asserts that at least one request of the specified type was received.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveReceivedRequest<TRequest>(this TestRelay relay)
        where TRequest : class
    {
        RelayAssertions.ShouldHaveHandled<TRequest>(relay);
    }

    /// <summary>
    /// Asserts that a specific number of requests of the specified type were received.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="expectedCount">The expected number of requests.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveReceivedRequest<TRequest>(this TestRelay relay, int expectedCount)
        where TRequest : class
    {
        RelayAssertions.ShouldHaveHandled<TRequest>(relay, expectedCount);
    }

    /// <summary>
    /// Asserts that no requests of the specified type were received.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldNotHaveReceivedRequest<TRequest>(this TestRelay relay)
        where TRequest : class
    {
        RelayAssertions.ShouldNotHaveHandled<TRequest>(relay);
    }

    /// <summary>
    /// Asserts that a notification matching the specified predicate was published.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="predicate">The predicate to match notifications against.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHavePublishedNotification<TNotification>(
        this TestRelay relay,
        Func<TNotification, bool> predicate)
        where TNotification : INotification
    {
        RelayAssertions.ShouldHavePublished(relay, predicate);
    }

    /// <summary>
    /// Asserts that a specific notification was published.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="expectedNotification">The expected notification instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHavePublishedNotification<TNotification>(
        this TestRelay relay,
        TNotification expectedNotification)
        where TNotification : INotification
    {
        var publishedNotifications = relay.PublishedNotifications;
        var matchingNotifications = publishedNotifications.OfType<TNotification>();

        if (!matchingNotifications.Any(n => Equals(n, expectedNotification)))
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected to find the specified notification of type '{typeof(TNotification).Name}' in published notifications, but it was not found. " +
                $"Published notifications: {string.Join(", ", publishedNotifications.Select(n => n.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that the TestRelay has no sent requests.
    /// </summary>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveNoSentRequests(this TestRelay relay)
    {
        var sentRequests = relay.SentRequests;

        if (sentRequests.Any())
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected no sent requests, but found {sentRequests.Count}. " +
                $"Sent requests: {string.Join(", ", sentRequests.Select(r => r.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that the TestRelay has no published notifications.
    /// </summary>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveNoPublishedNotifications(this TestRelay relay)
    {
        var publishedNotifications = relay.PublishedNotifications;

        if (publishedNotifications.Any())
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected no published notifications, but found {publishedNotifications.Count}. " +
                $"Published notifications: {string.Join(", ", publishedNotifications.Select(n => n.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that a specific request was sent.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="expectedRequest">The expected request instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveSentRequest<TRequest>(
        this TestRelay relay,
        TRequest expectedRequest)
        where TRequest : class
    {
        var sentRequests = relay.SentRequests;
        var matchingRequests = sentRequests.OfType<TRequest>();

        if (!matchingRequests.Any(r => Equals(r, expectedRequest)))
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected to find the specified request of type '{typeof(TRequest).Name}' in sent requests, but it was not found. " +
                $"Sent requests: {string.Join(", ", sentRequests.Select(r => r.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that the last sent request is of the specified type.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveSentLastRequest<TRequest>(this TestRelay relay)
        where TRequest : class
    {
        var sentRequests = relay.SentRequests.ToList();

        if (!sentRequests.Any())
        {
            throw new Xunit.Sdk.XunitException("Expected at least one sent request, but none were found.");
        }

        var lastRequest = sentRequests.Last();
        if (lastRequest.GetType() != typeof(TRequest))
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected the last sent request to be of type '{typeof(TRequest).Name}', but it was '{lastRequest.GetType().Name}'. " +
                $"Sent requests: {string.Join(", ", sentRequests.Select(r => r.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that the last published notification is of the specified type.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHavePublishedLastNotification<TNotification>(this TestRelay relay)
        where TNotification : INotification
    {
        var publishedNotifications = relay.PublishedNotifications.ToList();

        if (!publishedNotifications.Any())
        {
            throw new Xunit.Sdk.XunitException("Expected at least one published notification, but none were found.");
        }

        var lastNotification = publishedNotifications.Last();
        if (lastNotification.GetType() != typeof(TNotification))
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected the last published notification to be of type '{typeof(TNotification).Name}', but it was '{lastNotification.GetType().Name}'. " +
                $"Published notifications: {string.Join(", ", publishedNotifications.Select(n => n.GetType().Name))}");
        }
    }
}