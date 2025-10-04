# Relay.SourceGenerator

Roslyn source generator for the Relay mediator framework. This package provides compile-time code generation to eliminate runtime reflection overhead.

## Features

- Compile-time handler discovery and registration
- Optimized dispatcher generation
- DI container integration
- Endpoint metadata generation
- JSON schema generation

## Usage

Simply install this package alongside Relay.Core and the source generator will automatically run during compilation.

```xml
<PackageReference Include="Relay.Core" Version="1.0.0" />
<PackageReference Include="Relay.SourceGenerator" Version="1.0.0" />
```

The generator will automatically discover your handlers and generate the necessary registration code.

---

## 📁 Project Structure

The source generator is organized into logical folders for better maintainability:

### `/Core`
Core infrastructure and entry points for the source generator.

- **RelayIncrementalGenerator.cs** - Main incremental source generator entry point
- **RelayAnalyzer.cs** - Roslyn analyzer for compile-time validation
- **RelayCompilationContext.cs** - Compilation context wrapper with caching
- **SharedInterfaces.cs** - Common interfaces shared across the generator

### `/Generators`
Code generation components that produce different types of source code.

- **DIRegistrationGenerator.cs** - Generates dependency injection registration code
- **HandlerRegistryGenerator.cs** - Generates handler registry for runtime lookup
- **OptimizedDispatcherGenerator.cs** - Generates high-performance dispatchers
- **PipelineRegistryGenerator.cs** - Generates pipeline behavior registration
- **NotificationDispatcherGenerator.cs** - Generates notification dispatching code
- **EndpointMetadataGenerator.cs** - Generates endpoint metadata for HTTP APIs
- **JsonSchemaGenerator.cs** - Generates JSON schemas for requests/responses

### `/Discovery`
Components for discovering and analyzing handler methods and types.

- **HandlerDiscovery.cs** - Main discovery engine for finding handlers
- **HandlerDiscoveryResult.cs** - Result model containing discovered handlers
- **RelaySyntaxReceiver.cs** - Syntax receiver for incremental generation

### `/Diagnostics`
Diagnostic reporting and logging infrastructure.

- **IDiagnosticReporter.cs** - Interface and implementations for diagnostic reporting
- **DiagnosticDescriptors.cs** - Diagnostic descriptor definitions
- **DiagnosticReporterExtensions.cs** - Extension methods for common diagnostics
- **GeneratorLogger.cs** - Logging utilities for generator debugging

### `/Validation`
Configuration and code validation.

- **ConfigurationValidator.cs** - Validates Relay configuration and setup

## 🔄 Generation Pipeline

```
Source Code
    ↓
RelaySyntaxReceiver (Discovery)
    ↓
HandlerDiscovery (Analysis)
    ↓
HandlerDiscoveryResult
    ↓
Generators (Code Generation)
    ↓  ├─ DIRegistrationGenerator
    ↓  ├─ HandlerRegistryGenerator
    ↓  ├─ OptimizedDispatcherGenerator
    ↓  ├─ PipelineRegistryGenerator
    ↓  └─ Others...
    ↓
Generated Code
```

## 🎯 Design Principles

1. **Separation of Concerns** - Each folder has a specific responsibility
2. **Incremental Generation** - All generators support incremental compilation
3. **Performance Optimized** - Generators produce highly optimized code
4. **Diagnostic Rich** - Comprehensive error reporting and warnings
5. **Single Namespace** - All code uses `Relay.SourceGenerator` namespace for simplicity

## 📝 Notes

- All files maintain `Relay.SourceGenerator` namespace for cross-file references
- Folder structure provides logical organization without namespace complexity
- Each generator is independent and can be enabled/disabled via options
- Diagnostics are reported using Roslyn's diagnostic infrastructure