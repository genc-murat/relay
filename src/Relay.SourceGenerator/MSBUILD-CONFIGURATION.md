# MSBuild Configuration Guide for Relay.SourceGenerator

This guide explains how to configure Relay.SourceGenerator using MSBuild properties to control which generators run and how they behave.

---

## 📋 Table of Contents

1. [Quick Start](#quick-start)
2. [Available Configuration Properties](#available-configuration-properties)
3. [Configuration Methods](#configuration-methods)
4. [Common Scenarios](#common-scenarios)
5. [Best Practices](#best-practices)

---

## 🚀 Quick Start

### Method 1: Add to .csproj

Open your project file (`.csproj`) and add a `<PropertyGroup>`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <!-- Relay Generator Configuration -->
  <PropertyGroup>
    <RelayEnableEndpointMetadata>false</RelayEnableEndpointMetadata>
    <RelayEnablePipelineRegistry>false</RelayEnablePipelineRegistry>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Relay.SourceGenerator" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### Method 2: Use Directory.Build.props

Create a `Directory.Build.props` file in your solution root:

```xml
<Project>
  <PropertyGroup Label="Relay Generator Configuration">
    <RelayEnableEndpointMetadata>false</RelayEnableEndpointMetadata>
    <RelayIncludeDebugInfo>true</RelayIncludeDebugInfo>
  </PropertyGroup>
</Project>
```

This configuration will apply to all projects in the solution.

---

## ⚙️ Available Configuration Properties

### General Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RelayIncludeDebugInfo` | `bool` | `false` | Include debug information in generated code |
| `RelayIncludeDocumentation` | `bool` | `true` | Include XML documentation comments |
| `RelayEnableNullableContext` | `bool` | `true` | Enable nullable reference types context |
| `RelayUseAggressiveInlining` | `bool` | `true` | Use `[MethodImpl(AggressiveInlining)]` |
| `RelayCustomNamespace` | `string` | `Relay.Generated` | Custom namespace for generated code |

### Generator Enable/Disable Flags

| Property | Default | Generator | Purpose |
|----------|---------|-----------|---------|
| `RelayEnableDIGeneration` | `true` | DI Registration | Generates `AddRelay()` extension method |
| `RelayEnableHandlerRegistry` | `true` | Handler Registry | Generates handler metadata registry |
| `RelayEnableOptimizedDispatcher` | `true` | Optimized Dispatcher | Generates high-performance dispatcher |
| `RelayEnableNotificationDispatcher` | `true` | Notification Dispatcher | Generates notification handler |
| `RelayEnablePipelineRegistry` | `true` | Pipeline Registry | Generates pipeline behavior registry |
| `RelayEnableEndpointMetadata` | `true` | Endpoint Metadata | Generates endpoint metadata |

---

## 🔧 Configuration Methods

### 1. Per-Project Configuration (.csproj)

Best for: Project-specific settings

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <PropertyGroup Label="Relay Configuration">
    <!-- Disable generators not needed in this project -->
    <RelayEnableEndpointMetadata>false</RelayEnableEndpointMetadata>

    <!-- Use custom namespace -->
    <RelayCustomNamespace>MyCompany.Api.Generated</RelayCustomNamespace>
  </PropertyGroup>
</Project>
```

### 2. Solution-Wide Configuration (Directory.Build.props)

Best for: Consistent settings across all projects

Create `Directory.Build.props` in solution root:

```xml
<Project>
  <!-- Apply to all projects -->
  <PropertyGroup>
    <RelayIncludeDocumentation>true</RelayIncludeDocumentation>
    <RelayEnableNullableContext>true</RelayEnableNullableContext>
  </PropertyGroup>
</Project>
```

### 3. Conditional Configuration

Best for: Different settings per build configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <!-- Debug builds: Include debug info, disable optional generators -->
  <RelayIncludeDebugInfo>true</RelayIncludeDebugInfo>
  <RelayEnableEndpointMetadata>false</RelayEnableEndpointMetadata>
  <RelayEnablePipelineRegistry>false</RelayEnablePipelineRegistry>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <!-- Release builds: Optimize everything -->
  <RelayIncludeDebugInfo>false</RelayIncludeDebugInfo>
  <RelayUseAggressiveInlining>true</RelayUseAggressiveInlining>
</PropertyGroup>
```

### 4. Environment-Specific Configuration

```xml
<PropertyGroup Condition="'$(BuildEnvironment)' == 'CI'">
  <!-- CI builds: Minimal generation for faster builds -->
  <RelayEnableEndpointMetadata>false</RelayEnableEndpointMetadata>
</PropertyGroup>
```

---

## 💡 Common Scenarios

### Scenario 1: Minimal Build (Fastest Compilation)

Disable all optional generators for maximum build speed:

```xml
<PropertyGroup>
  <RelayEnableDIGeneration>true</RelayEnableDIGeneration>
  <RelayEnableHandlerRegistry>false</RelayEnableHandlerRegistry>
  <RelayEnableOptimizedDispatcher>true</RelayEnableOptimizedDispatcher>
  <RelayEnableNotificationDispatcher>false</RelayEnableNotificationDispatcher>
  <RelayEnablePipelineRegistry>false</RelayEnablePipelineRegistry>
  <RelayEnableEndpointMetadata>false</RelayEnableEndpointMetadata>
</PropertyGroup>
```

**Result**: Only core DI registration and dispatcher generation

### Scenario 2: Full-Featured Build

Enable all generators for complete functionality:

```xml
<PropertyGroup>
  <RelayEnableDIGeneration>true</RelayEnableDIGeneration>
  <RelayEnableHandlerRegistry>true</RelayEnableHandlerRegistry>
  <RelayEnableOptimizedDispatcher>true</RelayEnableOptimizedDispatcher>
  <RelayEnableNotificationDispatcher>true</RelayEnableNotificationDispatcher>
  <RelayEnablePipelineRegistry>true</RelayEnablePipelineRegistry>
  <RelayEnableEndpointMetadata>true</RelayEnableEndpointMetadata>
</PropertyGroup>
```

**Result**: All generators active (default behavior)

### Scenario 3: API-Only Project (No Notifications/Pipelines)

Disable notification and pipeline features:

```xml
<PropertyGroup>
  <RelayEnableNotificationDispatcher>false</RelayEnableNotificationDispatcher>
  <RelayEnablePipelineRegistry>false</RelayEnablePipelineRegistry>
</PropertyGroup>
```

### Scenario 4: Library Project (No DI Generation)

For class library projects that don't need DI registration:

```xml
<PropertyGroup>
  <RelayEnableDIGeneration>false</RelayEnableDIGeneration>
  <RelayEnableEndpointMetadata>false</RelayEnableEndpointMetadata>
</PropertyGroup>
```

### Scenario 5: Custom Namespace

Use your own namespace for generated code:

```xml
<PropertyGroup>
  <RelayCustomNamespace>$(RootNamespace).Generated</RelayCustomNamespace>
</PropertyGroup>
```

**Result**: Generated code will use `YourProjectName.Generated` namespace

---

## 📚 Best Practices

### 1. **Start with Defaults**

Don't configure anything unless you need to. The defaults work well for most projects:

```xml
<!-- No configuration needed - all generators enabled by default -->
<PackageReference Include="Relay.SourceGenerator" Version="1.0.0" />
```

### 2. **Disable Only What You Don't Use**

If your project doesn't use notifications, disable the notification dispatcher:

```xml
<PropertyGroup>
  <RelayEnableNotificationDispatcher>false</RelayEnableNotificationDispatcher>
</PropertyGroup>
```

### 3. **Use Conditional Properties for Different Builds**

```xml
<!-- Fast Debug builds -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <RelayIncludeDebugInfo>true</RelayIncludeDebugInfo>
  <RelayEnableEndpointMetadata>false</RelayEnableEndpointMetadata>
</PropertyGroup>

<!-- Optimized Release builds -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <RelayIncludeDebugInfo>false</RelayIncludeDebugInfo>
  <RelayUseAggressiveInlining>true</RelayUseAggressiveInlining>
</PropertyGroup>
```

### 4. **Document Your Configuration**

Add comments explaining why generators are disabled:

```xml
<PropertyGroup>
  <!-- Disabled: This project doesn't expose HTTP endpoints -->
  <RelayEnableEndpointMetadata>false</RelayEnableEndpointMetadata>

  <!-- Disabled: Pipeline behaviors handled manually -->
  <RelayEnablePipelineRegistry>false</RelayEnablePipelineRegistry>
</PropertyGroup>
```

### 5. **Use Directory.Build.props for Consistency**

For multi-project solutions, use `Directory.Build.props` to ensure consistent configuration:

```
MySolution/
├── Directory.Build.props          ← Solution-wide config
├── src/
│   ├── MyApi/
│   │   └── MyApi.csproj          ← Inherits from Directory.Build.props
│   └── MyLibrary/
│       └── MyLibrary.csproj      ← Inherits from Directory.Build.props
```

---

## 🔍 Troubleshooting

### Issue: Generator Not Running

**Problem**: A generator doesn't seem to run even though it's enabled.

**Solution**: Check that:
1. The property is set to `true` (not `True` or `1`)
2. No `Directory.Build.props` is overriding your setting
3. Clean and rebuild: `dotnet clean && dotnet build`

### Issue: Custom Namespace Not Working

**Problem**: Generated code still uses `Relay.Generated` namespace.

**Solution**: Ensure the property is spelled correctly:

```xml
<!-- ✅ Correct -->
<RelayCustomNamespace>MyApp.Generated</RelayCustomNamespace>

<!-- ❌ Wrong (typo) -->
<RelayCustomNameSpace>MyApp.Generated</RelayCustomNameSpace>
```

### Issue: Configuration Not Applied

**Problem**: Changes to properties don't seem to take effect.

**Solution**:
1. Clean the build: `dotnet clean`
2. Delete `obj/` and `bin/` folders
3. Rebuild: `dotnet build`

---

## 📖 Examples

### Example 1: Complete Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <PropertyGroup Label="Relay Source Generator Configuration">
    <!-- General Options -->
    <RelayIncludeDebugInfo>false</RelayIncludeDebugInfo>
    <RelayIncludeDocumentation>true</RelayIncludeDocumentation>
    <RelayEnableNullableContext>true</RelayEnableNullableContext>
    <RelayUseAggressiveInlining>true</RelayUseAggressiveInlining>
    <RelayCustomNamespace>$(RootNamespace).Generated</RelayCustomNamespace>

    <!-- Generator Flags -->
    <RelayEnableDIGeneration>true</RelayEnableDIGeneration>
    <RelayEnableHandlerRegistry>true</RelayEnableHandlerRegistry>
    <RelayEnableOptimizedDispatcher>true</RelayEnableOptimizedDispatcher>
    <RelayEnableNotificationDispatcher>true</RelayEnableNotificationDispatcher>
    <RelayEnablePipelineRegistry>true</RelayEnablePipelineRegistry>
    <RelayEnableEndpointMetadata>true</RelayEnableEndpointMetadata>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Relay.SourceGenerator" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### Example 2: Microservice Configuration

```xml
<PropertyGroup Label="Relay - Microservice Config">
  <!-- This microservice only handles requests, no notifications/pipelines -->
  <RelayEnableNotificationDispatcher>false</RelayEnableNotificationDispatcher>
  <RelayEnablePipelineRegistry>false</RelayEnablePipelineRegistry>
  <RelayEnableEndpointMetadata>true</RelayEnableEndpointMetadata>
</PropertyGroup>
```

---

## 📦 Property Reference

### Boolean Properties

Boolean properties accept the following values:

| Value | Result |
|-------|--------|
| `true`, `True`, `yes`, `1` | Enabled |
| `false`, `False`, `no`, `0` | Disabled |

### String Properties

String properties accept any valid string value:

```xml
<RelayCustomNamespace>My.Custom.Namespace</RelayCustomNamespace>
```

---

## 🚀 Performance Tips

1. **Disable Unused Generators**: Each disabled generator reduces build time
2. **Use Conditional Configuration**: Enable all generators for Release, minimal for Debug
3. **Disable Debug Info in Production**: Set `RelayIncludeDebugInfo` to `false` for Release builds

---

## 📞 Support

- **Documentation**: See `TASK-008-COMPLETED.md` for architecture details
- **Issues**: Report at https://github.com/anthropics/relay/issues
- **Examples**: See `RelaySourceGenerator.props.example`

---

**Last Updated**: 2025-10-17
**Version**: 1.0.0
