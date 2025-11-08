using System;
using System.Collections.Generic;

namespace Relay.Core.Testing;

/// <summary>
/// Represents a mock setup configuration.
/// </summary>
internal class MockSetup
{
    public SetupType SetupType { get; set; }
    public object ReturnValue { get; set; }
    public Delegate Func { get; set; }
    public Exception Exception { get; set; }
    public List<object> SequenceValues { get; set; }
    public int SequenceIndex { get; set; }

    public object Execute(object[] arguments)
    {
        switch (SetupType)
        {
            case SetupType.ReturnValue:
                return ReturnValue;

            case SetupType.Function:
                return Func.DynamicInvoke(arguments);

            case SetupType.Throw:
                throw Exception;

            case SetupType.Sequence:
                var value = SequenceValues[SequenceIndex % SequenceValues.Count];
                SequenceIndex++;
                return value;

            default:
                throw new InvalidOperationException($"Unknown setup type: {SetupType}");
        }
    }
}
