using System;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Validators;

/// <summary>
/// Context for parameter validation containing type checking delegate and expected type description.
/// </summary>
public sealed class ParameterValidationContext
{
    public Func<ITypeSymbol, bool> TypeValidator { get; }
    public string ExpectedTypeDescription { get; }

    public ParameterValidationContext(Func<ITypeSymbol, bool> typeValidator, string expectedTypeDescription)
    {
        TypeValidator = typeValidator;
        ExpectedTypeDescription = expectedTypeDescription;
    }
}
