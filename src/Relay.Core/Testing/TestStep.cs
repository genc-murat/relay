using System;
using System.Threading.Tasks;

namespace Relay.Core.Testing
{
    public class TestStep
    {
        public string Name { get; set; } = string.Empty;
        public StepType Type { get; set; }
        public object? Request { get; set; }
        public object? Notification { get; set; }
        public Func<Task<bool>>? VerificationFunc { get; set; }
        public TimeSpan? WaitTime { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Step name cannot be empty");

            switch (Type)
            {
                case StepType.SendRequest:
                    if (Request == null)
                        throw new InvalidOperationException($"Request is required for {Type} step: {Name}");
                    break;
                case StepType.PublishNotification:
                    if (Notification == null)
                        throw new InvalidOperationException($"Notification is required for {Type} step: {Name}");
                    break;
                case StepType.Verify:
                    if (VerificationFunc == null)
                        throw new InvalidOperationException($"VerificationFunc is required for {Type} step: {Name}");
                    break;
                case StepType.Wait:
                    if (WaitTime == null || WaitTime.Value <= TimeSpan.Zero)
                        throw new InvalidOperationException($"Valid WaitTime is required for {Type} step: {Name}");
                    break;
            }
        }
    }
}