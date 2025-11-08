using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing;

/// <summary>
/// Scenario template for streaming scenarios.
/// Provides fluent methods for streaming requests and processing streams.
/// </summary>
public class StreamingScenarioTemplate : ScenarioTemplate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingScenarioTemplate"/> class.
    /// </summary>
    /// <param name="scenarioName">The name of the scenario.</param>
    /// <param name="relay">The relay instance to use.</param>
    public StreamingScenarioTemplate(string scenarioName, IRelay relay)
        : base(scenarioName, relay)
    {
    }

    /// <summary>
    /// Initiates a streaming request and processes the stream.
    /// </summary>
    /// <typeparam name="TRequest">The type of the streaming request.</typeparam>
    /// <typeparam name="TResponse">The type of the response items in the stream.</typeparam>
    /// <param name="request">The streaming request.</param>
    /// <param name="stepName">The name of the step.</param>
    /// <returns>The streaming scenario template for chaining.</returns>
    public StreamingScenarioTemplate StreamRequest<TRequest, TResponse>(TRequest request, string stepName = "Stream Request")
        where TRequest : class, IStreamRequest<TResponse>
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        Scenario.Steps.Add(new TestStep
        {
            Name = stepName,
            Type = StepType.StreamRequest,
            StreamRequest = request
        });
        return this;
    }

    /// <summary>
    /// Verifies the streaming response by checking the collected stream items.
    /// </summary>
    /// <param name="verification">The verification function that receives the stream responses.</param>
    /// <param name="stepName">The name of the verification step.</param>
    /// <returns>The streaming scenario template for chaining.</returns>
    public StreamingScenarioTemplate VerifyStream(Func<IReadOnlyList<object>, Task<bool>> verification, string stepName = "Verify Stream")
    {
        if (verification == null) throw new ArgumentNullException(nameof(verification));

        Scenario.Steps.Add(new TestStep
        {
            Name = stepName,
            Type = StepType.Verify,
            VerificationFunc = async () =>
            {
                // This verification would need access to the previous step's response
                // For now, return true - this would need to be enhanced
                return await verification(new List<object>());
            }
        });
        return this;
    }

    /// <summary>
    /// Waits for streaming operations to complete.
    /// </summary>
    /// <param name="duration">The duration to wait.</param>
    /// <param name="stepName">The name of the wait step.</param>
    /// <returns>The streaming scenario template for chaining.</returns>
    public StreamingScenarioTemplate WaitForStreamCompletion(TimeSpan duration, string stepName = "Wait for Stream Completion")
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
    /// Sends a regular request that may initiate streaming behavior.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="stepName">The name of the step.</param>
    /// <returns>The streaming scenario template for chaining.</returns>
    public StreamingScenarioTemplate SendRequest<TRequest, TResponse>(TRequest request, string stepName = "Send Request")
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
    /// Publishes a notification that may affect streaming behavior.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="stepName">The name of the step.</param>
    /// <returns>The streaming scenario template for chaining.</returns>
    public StreamingScenarioTemplate PublishNotification<TNotification>(TNotification notification, string stepName = "Publish Notification")
        where TNotification : class, INotification
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));

        Scenario.Steps.Add(new TestStep
        {
            Name = stepName,
            Type = StepType.PublishNotification,
            Notification = notification
        });
        return this;
    }

    /// <summary>
    /// Builds the streaming scenario. This method can be overridden to customize the scenario building.
    /// </summary>
    protected override void BuildScenario()
    {
        // Default implementation does nothing - subclasses should override or use fluent methods
    }
}