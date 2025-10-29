# Diagnostic Configuration Guide

This guide explains how to configure diagnostic severity and suppression for Relay Source Generator diagnostics.

## Configuration Methods

### 1. MSBuild Properties

You can configure diagnostic severity using MSBuild properties in your `.csproj` file:

```xml
<PropertyGroup>
  <!-- Suppress specific diagnostics -->
  <RelaySuppressDiagnostics>RELAY_GEN_106,RELAY_GEN_107</RelaySuppressDiagnostics>
  
  <!-- Override severity for specific diagnostics -->
  <RelayDiagnosticSeverity_RELAY_GEN_104>error</RelayDiagnosticSeverity_RELAY_GEN_104>
  <RelayDiagnosticSeverity_RELAY_GEN_105>warning</RelayDiagnosticSeverity_RELAY_GEN_105>
  <RelayDiagnosticSeverity_RELAY_GEN_213>info</RelayDiagnosticSeverity_RELAY_GEN_213>
</PropertyGroup>
```

#### Severity Values

- `error` - Treat as compilation error
- `warning` - Treat as warning
- `info` - Treat as informational message
- `hidden` or `none` - Suppress the diagnostic

### 2. .editorconfig Support

You can also configure diagnostics using `.editorconfig` files:

```ini
[*.cs]
# Suppress specific diagnostics
dotnet_diagnostic.RELAY_GEN_106.severity = none
dotnet_diagnostic.RELAY_GEN_107.severity = suggestion

# Configure diagnostic severity
dotnet_diagnostic.RELAY_GEN_104.severity = warning
dotnet_diagnostic.RELAY_GEN_105.severity = error
dotnet_diagnostic.RELAY_GEN_213.severity = suggestion
```

#### .editorconfig Severity Values

- `error` - Compilation error
- `warning` - Warning
- `suggestion` - Informational
- `silent` or `none` - Suppressed

## Diagnostic Categories

### Error Diagnostics (RELAY_GEN_001-099)

These indicate critical issues that prevent code generation:

- `RELAY_GEN_001` - Source Generator Error
- `RELAY_GEN_002` - Invalid Handler Signature
- `RELAY_GEN_003` - Duplicate Handler Registration
- `RELAY_GEN_004` - Missing Relay.Core Reference
- `RELAY_GEN_005` - Named Handler Conflict

### Warning Diagnostics (RELAY_GEN_101-199)

These indicate potential issues or best practice violations:

- `RELAY_GEN_101` - Unused Handler
- `RELAY_GEN_102` - Performance Warning
- `RELAY_GEN_104` - Missing ConfigureAwait(false)
- `RELAY_GEN_105` - Sync-over-async detected
- `RELAY_GEN_106` - Private Handler
- `RELAY_GEN_107` - Internal Handler
- `RELAY_GEN_108` - Multiple Constructors
- `RELAY_GEN_109` - Constructor Value Type Parameter

### Configuration Diagnostics (RELAY_GEN_201-299)

These indicate configuration issues:

- `RELAY_GEN_201` - Duplicate Pipeline Order
- `RELAY_GEN_202` - Invalid Handler Return Type
- `RELAY_GEN_203` - Invalid Stream Handler Return Type
- `RELAY_GEN_204` - Invalid Notification Handler Return Type
- `RELAY_GEN_205` - Handler Missing Request Parameter
- `RELAY_GEN_206` - Handler Invalid Request Parameter Type
- `RELAY_GEN_207` - Handler Missing CancellationToken Parameter
- `RELAY_GEN_208` - Notification Handler Missing Parameter
- `RELAY_GEN_209` - Invalid Priority Value
- `RELAY_GEN_210` - No Handlers Found
- `RELAY_GEN_211` - Configuration Conflict
- `RELAY_GEN_212` - Invalid Pipeline Scope
- `RELAY_GEN_213` - Invalid Configuration Value (New)
- `RELAY_GEN_214` - Missing Required Attribute (New)
- `RELAY_GEN_215` - Obsolete Handler Pattern (New)
- `RELAY_GEN_216` - Performance Bottleneck Detected (New)

## Examples

### Example 1: Suppress Private Handler Warnings

If you intentionally use private handlers in your codebase:

```xml
<PropertyGroup>
  <RelaySuppressDiagnostics>RELAY_GEN_106</RelaySuppressDiagnostics>
</PropertyGroup>
```

Or in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.RELAY_GEN_106.severity = none
```

### Example 2: Enforce ConfigureAwait Best Practice

Make missing ConfigureAwait an error instead of a warning:

```xml
<PropertyGroup>
  <RelayDiagnosticSeverity_RELAY_GEN_104>error</RelayDiagnosticSeverity_RELAY_GEN_104>
</PropertyGroup>
```

Or in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.RELAY_GEN_104.severity = error
```

### Example 3: Suppress Multiple Diagnostics

```xml
<PropertyGroup>
  <RelaySuppressDiagnostics>RELAY_GEN_106;RELAY_GEN_107;RELAY_GEN_108</RelaySuppressDiagnostics>
</PropertyGroup>
```

### Example 4: Project-Specific Configuration

Different projects in the same solution can have different configurations:

**Project A (.csproj):**
```xml
<PropertyGroup>
  <!-- Strict configuration for production code -->
  <RelayDiagnosticSeverity_RELAY_GEN_104>error</RelayDiagnosticSeverity_RELAY_GEN_104>
  <RelayDiagnosticSeverity_RELAY_GEN_105>error</RelayDiagnosticSeverity_RELAY_GEN_105>
</PropertyGroup>
```

**Project B (.csproj):**
```xml
<PropertyGroup>
  <!-- Relaxed configuration for test code -->
  <RelaySuppressDiagnostics>RELAY_GEN_104,RELAY_GEN_105</RelaySuppressDiagnostics>
</PropertyGroup>
```

## Best Practices

1. **Use .editorconfig for team-wide standards**: Place configuration in `.editorconfig` at the repository root for consistent behavior across the team.

2. **Use MSBuild properties for project-specific overrides**: Override team standards in specific projects when needed.

3. **Document suppressions**: Add comments explaining why diagnostics are suppressed:

```xml
<PropertyGroup>
  <!-- Suppress RELAY_GEN_106: We use private handlers for internal implementation details -->
  <RelaySuppressDiagnostics>RELAY_GEN_106</RelaySuppressDiagnostics>
</PropertyGroup>
```

4. **Review suppressions regularly**: Periodically review suppressed diagnostics to ensure they're still necessary.

5. **Use severity escalation for critical issues**: Escalate warnings to errors for issues that should never be ignored:

```xml
<PropertyGroup>
  <!-- Treat sync-over-async as error to prevent deadlocks -->
  <RelayDiagnosticSeverity_RELAY_GEN_105>error</RelayDiagnosticSeverity_RELAY_GEN_105>
</PropertyGroup>
```

## Troubleshooting

### Configuration Not Taking Effect

1. **Clean and rebuild**: Configuration changes may require a clean rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Check property names**: Ensure property names are spelled correctly and use the exact diagnostic ID.

3. **Verify .editorconfig location**: The `.editorconfig` file must be in the project directory or a parent directory.

4. **Check for conflicts**: MSBuild properties take precedence over `.editorconfig` settings.

### Finding Diagnostic IDs

To find the diagnostic ID for a specific message:

1. Look at the error/warning message in the build output
2. The diagnostic ID is shown in brackets, e.g., `[RELAY_GEN_106]`
3. Refer to the diagnostic categories section above

## Related Documentation

- [MSBuild Configuration Guide](MSBUILD-CONFIGURATION.md)
- [Analyzer Rules](ANALYZER-RULES.md)
- [Relay Source Generator Wiki](https://github.com/MrDave1999/Relay/wiki)
