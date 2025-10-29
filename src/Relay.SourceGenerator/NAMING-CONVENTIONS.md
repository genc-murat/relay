# Relay Source Generator Naming Conventions

This document defines the naming conventions used throughout the Relay Source Generator codebase.

## General Principles

1. Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
2. Use clear, descriptive names that convey intent
3. Avoid abbreviations unless they are well-known (e.g., DI, HTTP, XML)
4. Use consistent terminology across the codebase

## Namespaces

- **Format**: `Relay.SourceGenerator.<Feature>`
- **Examples**:
  - `Relay.SourceGenerator.Core`
  - `Relay.SourceGenerator.Generators`
  - `Relay.SourceGenerator.Validation`
  - `Relay.SourceGenerator.Diagnostics`

## Classes

### Naming Patterns

- **Generators**: `<Purpose>Generator`
  - Examples: `DIRegistrationGenerator`, `OptimizedDispatcherGenerator`
- **Validators**: `<Target>Validator`
  - Examples: `HandlerValidator`, `AttributeValidator`
- **Helpers**: `<Purpose>Helper`
  - Examples: `TypeHelper`, `CodeGenerationHelper`
- **Extensions**: `<Type>Extensions`
  - Examples: `StringBuilderExtensions`, `SymbolExtensions`
- **Services**: `<Purpose>Service`
  - Examples: `DiagnosticService`, `ValidationService`
- **Contexts**: `<Purpose>Context`
  - Examples: `RelayCompilationContext`, `CodeGenerationContext`
- **Results**: `<Operation>Result`
  - Examples: `HandlerDiscoveryResult`, `ValidationResult`
- **Info/Data**: `<Entity>Info` or `<Entity>Data`
  - Examples: `HandlerInfo`, `AttributeData`

### Accessibility

- **Public classes**: PascalCase
- **Internal classes**: PascalCase
- **Private nested classes**: PascalCase

## Interfaces

- **Format**: `I<Name>`
- **Examples**:
  - `ICodeGenerator`
  - `IValidator<TInput, TResult>`
  - `IDiagnosticReporter`
  - `IServiceProvider`

## Methods

### Naming Patterns

- **Actions**: Use verbs
  - Examples: `Generate`, `Validate`, `Report`, `Create`
- **Queries**: Use descriptive names
  - Examples: `GetHandlers`, `FindType`, `IsValid`
- **Boolean queries**: Start with `Is`, `Has`, `Can`, `Should`
  - Examples: `IsValidHandler`, `HasAttribute`, `CanGenerate`
- **Async methods**: End with `Async`
  - Examples: `GenerateAsync`, `ValidateAsync`

### Accessibility

- **Public methods**: PascalCase
- **Private methods**: PascalCase
- **Local functions**: PascalCase

## Properties

- **Format**: PascalCase
- **Boolean properties**: Start with `Is`, `Has`, `Can`, `Should`
- **Examples**:
  - `GeneratorName`
  - `IsValid`
  - `HasErrors`
  - `CanGenerate`

## Fields

### Private Fields

- **Format**: `_camelCase` (with underscore prefix)
- **Examples**:
  - `_context`
  - `_diagnosticReporter`
  - `_semanticModelCache`

### Constants

- **Format**: PascalCase
- **Examples**:
  - `DefaultCapacity`
  - `MaxDegreeOfParallelism`

### Static Readonly Fields

- **Format**: PascalCase
- **Examples**:
  - `DefaultOptions`
  - `EmptyResult`

## Parameters

- **Format**: camelCase
- **Examples**:
  - `handlerInfo`
  - `diagnosticReporter`
  - `cancellationToken`

## Local Variables

- **Format**: camelCase
- **Examples**:
  - `result`
  - `builder`
  - `handlers`

## Type Parameters

- **Format**: `T<Description>` (single letter T followed by description)
- **Examples**:
  - `TRequest`
  - `TResponse`
  - `TInput`
  - `TResult`
  - `TService`

## Enums

### Enum Types

- **Format**: PascalCase (singular)
- **Examples**:
  - `HandlerType`
  - `RelayAttributeType`
  - `DiagnosticSeverity`

### Enum Values

- **Format**: PascalCase
- **Examples**:
  - `Request`
  - `Notification`
  - `Stream`

## Events

- **Format**: PascalCase (verb or verb phrase)
- **Examples**:
  - `GenerationCompleted`
  - `ValidationFailed`
  - `DiagnosticReported`

## Diagnostic IDs

- **Format**: `RELAY_GEN_<Number>`
- **Categories**:
  - `001-099`: Generator errors
  - `101-199`: Warnings
  - `201-299`: Configuration errors
  - `RELAY_INFO`: Informational messages

## Generated Code

### Namespaces

- **Default**: `Relay.Generated`
- **Configurable**: Via `RelayGeneratedNamespace` MSBuild property

### Classes

- **Format**: `Generated<Purpose>`
- **Examples**:
  - `GeneratedRequestDispatcher`
  - `GeneratedNotificationDispatcher`
  - `GeneratedHandlerRegistry`

### Methods

- **Format**: Descriptive, following C# conventions
- **Examples**:
  - `AddRelay`
  - `DispatchRequest`
  - `HandleNotification`

## File Names

- **Format**: Match the primary type name
- **Examples**:
  - `HandlerValidator.cs`
  - `ICodeGenerator.cs`
  - `StringBuilderExtensions.cs`

## Test Classes

- **Format**: `<TypeUnderTest>Tests`
- **Examples**:
  - `HandlerValidatorTests`
  - `DIRegistrationGeneratorTests`
  - `StringBuilderExtensionsTests`

## Test Methods

- **Format**: `<MethodUnderTest>_<Scenario>_<ExpectedResult>`
- **Examples**:
  - `Validate_WithValidHandler_ReturnsSuccess`
  - `Generate_WithNoHandlers_GeneratesBasicRegistration`
  - `IsValidHandler_WithInvalidSignature_ReturnsFalse`

## Common Abbreviations

The following abbreviations are acceptable:

- **DI**: Dependency Injection
- **HTTP**: Hypertext Transfer Protocol
- **XML**: Extensible Markup Language
- **JSON**: JavaScript Object Notation
- **API**: Application Programming Interface
- **URI**: Uniform Resource Identifier
- **URL**: Uniform Resource Locator
- **ID**: Identifier
- **DTO**: Data Transfer Object
- **CRUD**: Create, Read, Update, Delete

## Anti-Patterns to Avoid

1. **Hungarian notation**: Don't use type prefixes (e.g., `strName`, `intCount`)
2. **Single-letter variables**: Except for loop counters (`i`, `j`, `k`) and type parameters (`T`)
3. **Unclear abbreviations**: Avoid `mgr`, `ctx`, `svc` unless in very local scope
4. **Inconsistent terminology**: Use the same term for the same concept throughout

## Examples

### Good Names

```csharp
// Classes
public class HandlerDiscoveryEngine { }
public class OptimizedDispatcherGenerator : BaseCodeGenerator { }

// Interfaces
public interface ICodeGenerator { }
public interface IValidator<TInput, TResult> { }

// Methods
public ValidationResult Validate(HandlerInfo handler) { }
public bool CanGenerate(HandlerDiscoveryResult result) { }
public ITypeSymbol? GetResponseType(ITypeSymbol requestType) { }

// Properties
public string GeneratorName { get; }
public bool IsValid { get; }
public int Priority { get; }

// Fields
private readonly IDiagnosticReporter _diagnosticReporter;
private const int DefaultCapacity = 1024;

// Parameters
public void Generate(HandlerDiscoveryResult result, GenerationOptions options) { }

// Local variables
var handlers = discoveryResult.Handlers;
var builder = new StringBuilder();
```

### Bad Names

```csharp
// Too short/unclear
public class HDEngine { }  // Use HandlerDiscoveryEngine
public class Gen { }       // Use Generator or specific name

// Inconsistent
public class HandlerValidator { }
public class ValidatorForAttributes { }  // Use AttributeValidator

// Hungarian notation
string strName;  // Use name
int intCount;    // Use count

// Unclear abbreviations
var ctx = new Context();  // Use context in most cases
var mgr = new Manager();  // Use manager
```

## Refactoring Guidelines

When refactoring existing code to follow these conventions:

1. **Prioritize public APIs**: Start with public interfaces and classes
2. **Maintain backward compatibility**: Use `[Obsolete]` for breaking changes
3. **Update documentation**: Ensure XML comments reflect new names
4. **Update tests**: Rename test methods to match new conventions
5. **Be consistent**: Apply conventions uniformly across related code
