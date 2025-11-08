using System;
using System.Threading.Tasks;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing;

/// <summary>
/// Scenario template for event-driven architectures.
/// Provides fluent methods for publishing events and verifying side effects.
/// </summary>
public class EventDrivenScenarioTemplate : ScenarioTemplate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventDrivenScenarioTemplate"/> class.
    /// </summary>
    /// <param name="scenarioName">The name of the scenario.</param>
    /// <param name="relay">The relay instance to use.</param>
    public EventDrivenScenarioTemplate(string scenarioName, IRelay relay)
        : base(scenarioName, relay)
    {
    }

    /// <summary>
    /// Publishes an event to trigger event-driven behavior.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="stepName">The name of the step.</param>
    /// <returns>The event-driven scenario template for chaining.</returns>
    public EventDrivenScenarioTemplate PublishEvent<TEvent>(TEvent @event, string stepName = "Publish Event")
        where TEvent : class, INotification
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        Scenario.Steps.Add(new TestStep
        {
            Name = stepName,
            Type = StepType.PublishNotification,
            Notification = @event
        });
        return this;
    }

    /// <summary>
    /// Publishes multiple events in sequence.
    /// </summary>
    /// <typeparam name="TEvent">The type of the events.</typeparam>
    /// <param name="events">The events to publish.</param>
    /// <param name="stepName">The name of the step.</param>
    /// <returns>The event-driven scenario template for chaining.</returns>
    public EventDrivenScenarioTemplate PublishEvents<TEvent>(params TEvent[] events)
        where TEvent : class, INotification
    {
        if (events == null) throw new ArgumentNullException(nameof(events));

        for (int i = 0; i < events.Length; i++)
        {
            var @event = events[i];
            if (@event == null) throw new ArgumentException($"Event at index {i} cannot be null", nameof(events));

            PublishEvent(@event, $"Publish Event {i + 1}");
        }
        return this;
    }

    /// <summary>
    /// Verifies that expected side effects occurred after event publication.
    /// </summary>
    /// <param name="verification">The verification function.</param>
    /// <param name="stepName">The name of the verification step.</param>
    /// <returns>The event-driven scenario template for chaining.</returns>
    public EventDrivenScenarioTemplate VerifySideEffects(Func<Task<bool>> verification, string stepName = "Verify Side Effects")
    {
        if (verification == null) throw new ArgumentNullException(nameof(verification));

        Scenario.Steps.Add(new TestStep
        {
            Name = stepName,
            Type = StepType.Verify,
            VerificationFunc = verification
        });
        return this;
    }

    /// <summary>
    /// Waits for event processing to complete.
    /// </summary>
    /// <param name="duration">The duration to wait.</param>
    /// <param name="stepName">The name of the wait step.</param>
    /// <returns>The event-driven scenario template for chaining.</returns>
    public EventDrivenScenarioTemplate WaitForEventProcessing(TimeSpan duration, string stepName = "Wait for Event Processing")
    {
        Scenario.Steps.Add(new TestStep
        {
            Name = stepName,
            Type = StepType.Wait,
            WaitTime = duration
        });
        return this;
    }

    /// <summary>
    /// Sends a request that may trigger events as a side effect.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="stepName">The name of the step.</param>
    /// <returns>The event-driven scenario template for chaining.</returns>
    public EventDrivenScenarioTemplate SendRequest<TRequest, TResponse>(TRequest request, string stepName = "Send Request")
        where TRequest : class, IRequest<TResponse>
    {
        Scenario.Steps.Add(new TestStep
        {
            Name = stepName,
            Type = StepType.SendRequest,
            Request = request
        });
        return this;
    }

    /// <summary>
    /// Builds the event-driven scenario. This method can be overridden to customize the scenario building.
    /// </summary>
    protected override void BuildScenario()
    {
        // Default implementation does nothing - subclasses should override or use fluent methods
    }
}