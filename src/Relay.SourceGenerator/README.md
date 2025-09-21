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