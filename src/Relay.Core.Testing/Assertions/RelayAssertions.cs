using System;
using System.Linq;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing;

/// <summary>
/// Enhanced assertion helpers for Relay testing scenarios.
/// </summary>
public static class RelayAssertions
{
    /// <summary>
    /// Asserts that a specific request type was handled by the TestRelay.
    /// </summary>
    /// <typeparam name="TRequest">The type of request that should have been handled.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveHandled<TRequest>(this TestRelay relay)
        where TRequest : class
    {
        var sentRequests = relay.SentRequests;
        var handledRequests = sentRequests.OfType<TRequest>();

        if (!handledRequests.Any())
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected to find at least one request of type '{typeof(TRequest).Name}' in sent requests, but none were found. " +
                $"Sent requests: {string.Join(", ", sentRequests.Select(r => r.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that a specific request type was handled by the TestRelay a specific number of times.
    /// </summary>
    /// <typeparam name="TRequest">The type of request that should have been handled.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="expectedCount">The expected number of times the request should have been handled.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveHandled<TRequest>(this TestRelay relay, int expectedCount)
        where TRequest : class
    {
        var sentRequests = relay.SentRequests;
        var handledRequests = sentRequests.OfType<TRequest>().ToList();
        var actualCount = handledRequests.Count;

        if (actualCount != expectedCount)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected to find {expectedCount} request(s) of type '{typeof(TRequest).Name}', but found {actualCount}. " +
                $"Sent requests: {string.Join(", ", sentRequests.Select(r => r.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that a specific notification type was published by the TestRelay.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification that should have been published.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHavePublished<TNotification>(this TestRelay relay)
        where TNotification : INotification
    {
        var publishedNotifications = relay.PublishedNotifications;
        var matchingNotifications = publishedNotifications.OfType<TNotification>();

        if (!matchingNotifications.Any())
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected to find at least one notification of type '{typeof(TNotification).Name}' in published notifications, but none were found. " +
                $"Published notifications: {string.Join(", ", publishedNotifications.Select(n => n.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that a specific notification type was published by the TestRelay a specific number of times.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification that should have been published.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="expectedCount">The expected number of times the notification should have been published.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHavePublished<TNotification>(this TestRelay relay, int expectedCount)
        where TNotification : INotification
    {
        var publishedNotifications = relay.PublishedNotifications;
        var matchingNotifications = publishedNotifications.OfType<TNotification>().ToList();
        var actualCount = matchingNotifications.Count;

        if (actualCount != expectedCount)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected to find {expectedCount} notification(s) of type '{typeof(TNotification).Name}', but found {actualCount}. " +
                $"Published notifications: {string.Join(", ", publishedNotifications.Select(n => n.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that a notification matching the specified predicate was published.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="predicate">The predicate to match notifications against.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHavePublished<TNotification>(
        this TestRelay relay,
        Func<TNotification, bool> predicate)
        where TNotification : INotification
    {
        var publishedNotifications = relay.PublishedNotifications;
        var matchingNotifications = publishedNotifications.OfType<TNotification>().Where(predicate);

        if (!matchingNotifications.Any())
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected to find at least one notification of type '{typeof(TNotification).Name}' matching the specified predicate, but none were found. " +
                $"Published notifications: {string.Join(", ", publishedNotifications.Select(n => n.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that requests were handled in the specified order.
    /// </summary>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="requestTypes">The expected order of request types.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveHandledInOrder(this TestRelay relay, params Type[] requestTypes)
    {
        var sentRequests = relay.SentRequests.ToList();

        if (sentRequests.Count < requestTypes.Length)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected at least {requestTypes.Length} requests, but only {sentRequests.Count} were sent. " +
                $"Sent requests: {string.Join(", ", sentRequests.Select(r => r.GetType().Name))}");
        }

        for (int i = 0; i < requestTypes.Length; i++)
        {
            var expectedType = requestTypes[i];
            var actualType = sentRequests[i].GetType();

            if (actualType != expectedType)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Expected request at position {i} to be of type '{expectedType.Name}', but was '{actualType.Name}'. " +
                    $"Expected order: {string.Join(", ", requestTypes.Select(t => t.Name))} " +
                    $"Actual order: {string.Join(", ", sentRequests.Select(r => r.GetType().Name))}");
            }
        }
    }

    /// <summary>
    /// Asserts that notifications were published in the specified order.
    /// </summary>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="notificationTypes">The expected order of notification types.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHavePublishedInOrder(this TestRelay relay, params Type[] notificationTypes)
    {
        var publishedNotifications = relay.PublishedNotifications.ToList();

        if (publishedNotifications.Count < notificationTypes.Length)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected at least {notificationTypes.Length} notifications, but only {publishedNotifications.Count} were published. " +
                $"Published notifications: {string.Join(", ", publishedNotifications.Select(n => n.GetType().Name))}");
        }

        for (int i = 0; i < notificationTypes.Length; i++)
        {
            var expectedType = notificationTypes[i];
            var actualType = publishedNotifications[i].GetType();

            if (actualType != expectedType)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Expected notification at position {i} to be of type '{expectedType.Name}', but was '{actualType.Name}'. " +
                    $"Expected order: {string.Join(", ", notificationTypes.Select(t => t.Name))} " +
                    $"Actual order: {string.Join(", ", publishedNotifications.Select(n => n.GetType().Name))}");
            }
        }
    }

    /// <summary>
    /// Asserts that no requests of the specified type were handled.
    /// </summary>
    /// <typeparam name="TRequest">The type of request that should not have been handled.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldNotHaveHandled<TRequest>(this TestRelay relay)
        where TRequest : class
    {
        var sentRequests = relay.SentRequests;
        var handledRequests = sentRequests.OfType<TRequest>();

        if (handledRequests.Any())
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected no requests of type '{typeof(TRequest).Name}' to be handled, but found {handledRequests.Count()}. " +
                $"Sent requests: {string.Join(", ", sentRequests.Select(r => r.GetType().Name))}");
        }
    }

    /// <summary>
    /// Asserts that no notifications of the specified type were published.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification that should not have been published.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldNotHavePublished<TNotification>(this TestRelay relay)
        where TNotification : INotification
    {
        var publishedNotifications = relay.PublishedNotifications;
        var matchingNotifications = publishedNotifications.OfType<TNotification>();

        if (matchingNotifications.Any())
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected no notifications of type '{typeof(TNotification).Name}' to be published, but found {matchingNotifications.Count()}. " +
                $"Published notifications: {string.Join(", ", publishedNotifications.Select(n => n.GetType().Name))}");
        }
    }
}