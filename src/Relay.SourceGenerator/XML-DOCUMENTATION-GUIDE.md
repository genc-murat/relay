# XML Documentation Guide for Relay Source Generator

This guide provides standards and templates for XML documentation comments in the Relay Source Generator codebase.

## General Principles

1. **All public APIs must have XML documentation**
2. **Documentation should explain "why" not just "what"**
3. **Include examples for complex APIs**
4. **Keep documentation up-to-date with code changes**
5. **Use proper grammar and punctuation**

## XML Documentation Tags

### Summary

The `<summary>` tag provides a brief description of the type or member.

```csharp
/// <summary>
/// Validates handler method signatures for proper async patterns and parameter types.
/// </summary>
public class HandlerValidator { }
```

### Remarks

The `<remarks>` tag provides additional detailed information.

```csharp
/// <summary>
/// Generates optimized dispatcher code for request handlers.
/// </summary>
/// <remarks>
/// This generator uses pattern matching and aggressive inlining to achieve
/// O(1) dispatch performance. It generates type-specific dispatch methods
/// for each request type discovered in the compilation.
/// </remarks>
public class OptimizedDispatcherGenerator : BaseCodeGenerator { }
```

### Param

The `<param>` tag describes a method parameter.

```csharp
/// <summary>
/// Validates a handler method signature.
/// </summary>
/// <param name="methodSymbol">The method symbol to validate</param>
/// <param name="diagnosticReporter">The diagnostic reporter for errors</param>
/// <returns>A validation result indicating success or failure</returns>
public ValidationResult Validate(IMethodSymbol methodSymbol, IDiagnosticReporter diagnosticReporter)
```

### Returns

The `<returns>` tag describes the return value.

```csharp
/// <summary>
/// Gets the response type from a request type symbol.
/// </summary>
/// <param name="requestType">The request type symbol</param>
/// <returns>The response type if found, null otherwise</returns>
public ITypeSymbol? GetResponseType(ITypeSymbol requestType)
```

### Exception

The `<exception>` tag documents exceptions that can be thrown.

```csharp
/// <summary>
/// Generates source code for the given discovery result.
/// </summary>
/// <param name="result">The handler discovery result</param>
/// <param name="options">Generation options</param>
/// <returns>The generated source code</returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="result"/> or <paramref name="options"/> is null.
/// </exception>
/// <exception cref="InvalidOperationException">
/// Thrown when the generator cannot produce valid code for the given input.
/// </exception>
public string Generate(HandlerDiscoveryResult result, GenerationOptions options)
```

### Example

The `<example>` tag provides usage examples.

```csharp
/// <summary>
/// Extension methods for StringBuilder to reduce code duplication.
/// </summary>
/// <example>
/// <code>
/// var builder = new StringBuilder();
/// builder.AppendIndentedLine(1, "public class MyClass")
///        .AppendIndentedLine(1, "{")
///        .AppendIndentedLine(2, "public string Name { get; set; }")
///        .AppendIndentedLine(1, "}");
/// </code>
/// </example>
public static class StringBuilderExtensions { }
```

### See/SeeAlso

The `<see>` and `<seealso>` tags create links to other types or members.

```csharp
/// <summary>
/// Base class for all code generators.
/// </summary>
/// <seealso cref="ICodeGenerator"/>
/// <seealso cref="GenerationOptions"/>
public abstract class BaseCodeGenerator : ICodeGenerator
{
    /// <summary>
    /// Generates code using the specified options.
    /// See <see cref="GenerationOptions"/> for available configuration.
    /// </summary>
    public abstract string Generate(HandlerDiscoveryResult result, GenerationOptions options);
}
```

### TypeParam

The `<typeparam>` tag describes generic type parameters.

```csharp
/// <summary>
/// Base interface for all validators in the Relay source generator.
/// </summary>
/// <typeparam name="TInput">The type of input to validate</typeparam>
/// <typeparam name="TResult">The type of validation result</typeparam>
public interface IValidator<TInput, TResult>
{
    /// <summary>
    /// Validates the input and returns a validation result.
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>The validation result</returns>
    TResult Validate(TInput input);
}
```

### Value

The `<value>` tag describes a property.

```csharp
/// <summary>
/// Gets the name of this generator.
/// </summary>
/// <value>
/// A string containing the generator name, used for logging and diagnostics.
/// </value>
public string GeneratorName { get; }
```

### Inheritdoc

The `<inheritdoc/>` tag inherits documentation from a base class or interface.

```csharp
public class DIRegistrationGenerator : BaseCodeGenerator
{
    /// <inheritdoc/>
    public override string GeneratorName => "DI Registration Generator";

    /// <inheritdoc/>
    public override string Generate(HandlerDiscoveryResult result, GenerationOptions options)
    {
        // Implementation
    }
}
```

## Documentation Templates

### Class Template

```csharp
/// <summary>
/// [Brief description of what the class does]
/// </summary>
/// <remarks>
/// [Optional: Additional details, design decisions, usage notes]
/// </remarks>
/// <example>
/// [Optional: Usage example]
/// <code>
/// var instance = new MyClass();
/// instance.DoSomething();
/// </code>
/// </example>
public class MyClass
{
}
```

### Interface Template

```csharp
/// <summary>
/// [Brief description of the interface contract]
/// </summary>
/// <remarks>
/// [Optional: Implementation guidelines, design patterns]
/// </remarks>
public interface IMyInterface
{
}
```

### Method Template

```csharp
/// <summary>
/// [Brief description of what the method does]
/// </summary>
/// <param name="parameter1">[Description of parameter1]</param>
/// <param name="parameter2">[Description of parameter2]</param>
/// <returns>[Description of return value]</returns>
/// <exception cref="ExceptionType">[When this exception is thrown]</exception>
/// <remarks>
/// [Optional: Additional details, performance notes, thread safety]
/// </remarks>
/// <example>
/// [Optional: Usage example]
/// <code>
/// var result = MyMethod(arg1, arg2);
/// </code>
/// </example>
public ReturnType MyMethod(Type1 parameter1, Type2 parameter2)
{
}
```

### Property Template

```csharp
/// <summary>
/// Gets or sets [description of what the property represents].
/// </summary>
/// <value>
/// [Optional: Detailed description of the property value]
/// </value>
/// <remarks>
/// [Optional: Additional notes, default values, constraints]
/// </remarks>
public string MyProperty { get; set; }
```

### Constructor Template

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="MyClass"/> class.
/// </summary>
/// <param name="parameter1">[Description of parameter1]</param>
/// <param name="parameter2">[Description of parameter2]</param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="parameter1"/> is null.
/// </exception>
public MyClass(Type1 parameter1, Type2 parameter2)
{
}
```

### Enum Template

```csharp
/// <summary>
/// [Description of what the enum represents]
/// </summary>
public enum MyEnum
{
    /// <summary>
    /// [Description of this enum value]
    /// </summary>
    Value1,

    /// <summary>
    /// [Description of this enum value]
    /// </summary>
    Value2
}
```

## Best Practices

### DO

✅ **Use complete sentences with proper punctuation**
```csharp
/// <summary>
/// Validates the handler signature and reports diagnostics.
/// </summary>
```

✅ **Be specific and descriptive**
```csharp
/// <summary>
/// Gets the response type from an IRequest&lt;TResponse&gt; interface implementation.
/// </summary>
```

✅ **Document null behavior**
```csharp
/// <summary>
/// Finds a type by its full name.
/// </summary>
/// <param name="fullTypeName">The full type name including namespace</param>
/// <returns>The type symbol if found, null otherwise</returns>
```

✅ **Document thread safety**
```csharp
/// <summary>
/// Gets the semantic model for a syntax tree with thread-safe caching.
/// </summary>
/// <remarks>
/// This method is thread-safe and can be called concurrently from multiple threads.
/// </remarks>
```

✅ **Document performance characteristics**
```csharp
/// <summary>
/// Generates optimized dispatcher code using pattern matching.
/// </summary>
/// <remarks>
/// This generator produces O(1) dispatch performance by using switch expressions
/// and aggressive inlining. Memory allocations are minimized through StringBuilder pooling.
/// </remarks>
```

### DON'T

❌ **Don't just repeat the member name**
```csharp
// Bad
/// <summary>
/// The generator name.
/// </summary>
public string GeneratorName { get; }

// Good
/// <summary>
/// Gets the unique name of this generator, used for logging and diagnostics.
/// </summary>
public string GeneratorName { get; }
```

❌ **Don't use incomplete sentences**
```csharp
// Bad
/// <summary>
/// Validates handler
/// </summary>

// Good
/// <summary>
/// Validates the handler method signature.
/// </summary>
```

❌ **Don't forget to document exceptions**
```csharp
// Bad
public void Process(string input)
{
    if (input == null) throw new ArgumentNullException(nameof(input));
}

// Good
/// <summary>
/// Processes the input string.
/// </summary>
/// <param name="input">The input to process</param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="input"/> is null.
/// </exception>
public void Process(string input)
{
    if (input == null) throw new ArgumentNullException(nameof(input));
}
```

❌ **Don't use vague descriptions**
```csharp
// Bad
/// <summary>
/// Does something with the handler.
/// </summary>

// Good
/// <summary>
/// Validates the handler method signature and reports any diagnostics.
/// </summary>
```

## Special Cases

### Extension Methods

```csharp
/// <summary>
/// Appends an indented line to the StringBuilder.
/// </summary>
/// <param name="builder">The StringBuilder instance</param>
/// <param name="indentLevel">The indentation level (each level is 4 spaces)</param>
/// <param name="line">The line to append</param>
/// <returns>The StringBuilder for method chaining</returns>
public static StringBuilder AppendIndentedLine(this StringBuilder builder, int indentLevel, string line)
```

### Async Methods

```csharp
/// <summary>
/// Asynchronously validates the handler and reports diagnostics.
/// </summary>
/// <param name="handler">The handler to validate</param>
/// <param name="cancellationToken">A token to cancel the operation</param>
/// <returns>
/// A task that represents the asynchronous operation.
/// The task result contains the validation result.
/// </returns>
public async Task<ValidationResult> ValidateAsync(HandlerInfo handler, CancellationToken cancellationToken)
```

### Generic Methods

```csharp
/// <summary>
/// Gets a service of the specified type from the service provider.
/// </summary>
/// <typeparam name="TService">The type of service to retrieve</typeparam>
/// <returns>The service instance</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when the service of type <typeparamref name="TService"/> is not registered.
/// </exception>
public TService GetService<TService>() where TService : class
```

### Obsolete Members

```csharp
/// <summary>
/// Generates DI registrations for the discovered handlers.
/// </summary>
/// <param name="discoveryResult">The handler discovery result</param>
/// <returns>The generated source code</returns>
/// <remarks>
/// This method is obsolete and will be removed in a future version.
/// Use <see cref="Generate(HandlerDiscoveryResult, GenerationOptions)"/> instead.
/// </remarks>
[Obsolete("Use Generate(HandlerDiscoveryResult, GenerationOptions) instead.")]
public string GenerateDIRegistrations(HandlerDiscoveryResult discoveryResult)
```

## Documentation Review Checklist

Before committing code, ensure:

- [ ] All public types have `<summary>` tags
- [ ] All public methods have `<summary>` and `<param>` tags
- [ ] All public methods with return values have `<returns>` tags
- [ ] All exceptions are documented with `<exception>` tags
- [ ] Complex APIs have `<example>` tags
- [ ] Generic type parameters have `<typeparam>` tags
- [ ] Documentation uses complete sentences with proper punctuation
- [ ] Documentation is clear and describes "why" not just "what"
- [ ] Cross-references use `<see>` and `<seealso>` tags
- [ ] Thread safety is documented where relevant
- [ ] Performance characteristics are documented where relevant
