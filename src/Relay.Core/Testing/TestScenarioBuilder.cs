using System;
using System.Threading.Tasks;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing
{
    /// <summary>
    /// Builder for test scenarios.
    /// </summary>
    public class TestScenarioBuilder
    {
        private readonly TestScenario _scenario;
        private readonly IRelay _relay;

        public TestScenarioBuilder(TestScenario scenario, IRelay relay)
        {
            _scenario = scenario;
            _relay = relay;
        }

        public TestScenarioBuilder SendRequest<TRequest>(TRequest request, string stepName = "Send Request")
            where TRequest : class
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(stepName)) throw new ArgumentException("Step name cannot be empty", nameof(stepName));

            _scenario.Steps.Add(new TestStep
            {
                Name = stepName,
                Type = StepType.SendRequest,
                Request = request
            });
            return this;
        }

        public TestScenarioBuilder PublishNotification<TNotification>(TNotification notification, string stepName = "Publish Notification")
            where TNotification : INotification
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            if (string.IsNullOrWhiteSpace(stepName)) throw new ArgumentException("Step name cannot be empty", nameof(stepName));

            _scenario.Steps.Add(new TestStep
            {
                Name = stepName,
                Type = StepType.PublishNotification,
                Notification = notification
            });
            return this;
        }

        public TestScenarioBuilder StreamRequest<TRequest>(TRequest request, string stepName = "Stream Request")
            where TRequest : class
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(stepName)) throw new ArgumentException("Step name cannot be empty", nameof(stepName));

            _scenario.Steps.Add(new TestStep
            {
                Name = stepName,
                Type = StepType.StreamRequest,
                StreamRequest = request
            });
            return this;
        }

        public TestScenarioBuilder Verify(Func<Task<bool>> verificationFunc, string stepName = "Verify")
        {
            _scenario.Steps.Add(new TestStep
            {
                Name = stepName,
                Type = StepType.Verify,
                VerificationFunc = verificationFunc
            });
            return this;
        }

        public TestScenarioBuilder Wait(TimeSpan duration, string stepName = "Wait")
        {
            _scenario.Steps.Add(new TestStep
            {
                Name = stepName,
                Type = StepType.Wait,
                WaitTime = duration
            });
            return this;
        }
    }
}