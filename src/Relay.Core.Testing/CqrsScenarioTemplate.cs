using System;
using System.Threading.Tasks;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing;

/// <summary>
/// Scenario template for Command Query Responsibility Segregation (CQRS) patterns.
/// Provides fluent methods for sending commands and queries.
/// </summary>
public class CqrsScenarioTemplate : ScenarioTemplate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CqrsScenarioTemplate"/> class.
    /// </summary>
    /// <param name="scenarioName">The name of the scenario.</param>
    /// <param name="relay">The relay instance to use.</param>
    public CqrsScenarioTemplate(string scenarioName, IRelay relay)
        : base(scenarioName, relay)
    {
    }

    /// <summary>
    /// Sends a command and optionally verifies the response.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <param name="stepName">The name of the step.</param>
    /// <returns>The CQRS scenario template for chaining.</returns>
    public CqrsScenarioTemplate SendCommand<TCommand, TResponse>(TCommand command, string stepName = "Send Command")
        where TCommand : class, IRequest<TResponse>
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        Scenario.Steps.Add(new TestStep
        {
            Name = stepName,
            Type = StepType.SendRequest,
            Request = command
        });
        return this;
    }

    /// <summary>
    /// Sends a query and captures the response for later verification.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="query">The query to send.</param>
    /// <param name="stepName">The name of the step.</param>
    /// <returns>The CQRS scenario template for chaining.</returns>
    public CqrsScenarioTemplate SendQuery<TQuery, TResponse>(TQuery query, string stepName = "Send Query")
        where TQuery : class, IRequest<TResponse>
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        Scenario.Steps.Add(new TestStep
        {
            Name = stepName,
            Type = StepType.SendRequest,
            Request = query
        });
        return this;
    }

    /// <summary>
    /// Adds a verification step to check the state after commands/queries.
    /// </summary>
    /// <param name="verification">The verification function.</param>
    /// <param name="stepName">The name of the verification step.</param>
    /// <returns>The CQRS scenario template for chaining.</returns>
    public CqrsScenarioTemplate VerifyState(Func<Task<bool>> verification, string stepName = "Verify State")
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
    /// Adds a wait step for asynchronous operations to complete.
    /// </summary>
    /// <param name="duration">The duration to wait.</param>
    /// <param name="stepName">The name of the wait step.</param>
    /// <returns>The CQRS scenario template for chaining.</returns>
    public CqrsScenarioTemplate WaitForProcessing(TimeSpan duration, string stepName = "Wait for Processing")
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
    /// Builds the CQRS scenario. This method can be overridden to customize the scenario building.
    /// </summary>
    protected override void BuildScenario()
    {
        // Default implementation does nothing - subclasses should override or use fluent methods
    }
}