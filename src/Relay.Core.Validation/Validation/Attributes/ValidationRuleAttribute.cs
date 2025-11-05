using System;

namespace Relay.Core.Validation.Attributes
{
    /// <summary>
    /// Attribute to mark classes as validation rules for specific request types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ValidationRuleAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the priority of the validation rule. Lower values execute first.
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether this rule should run even if previous rules have failed.
        /// </summary>
        public bool ContinueOnError { get; set; } = false;
    }
}
