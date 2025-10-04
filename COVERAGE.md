# Code Coverage Report Generation

This directory contains a script to generate clean code coverage reports for the Relay project.

## Quick Start

Generate a coverage report with fresh test runs:
```powershell
.\generate-coverage-report.ps1
```

## Script Options

- **-SkipTests**: Skip running tests and use existing coverage data
  ```powershell
  .\generate-coverage-report.ps1 -SkipTests
  ```

- **-ReportType**: Specify the report format (default: Html)
  ```powershell
  .\generate-coverage-report.ps1 -ReportType "Html;Badges;Cobertura"
  ```

## What the Script Does

1. **Cleans old test results**: Removes all previous TestResults directories to prevent stale data warnings
2. **Runs tests with coverage**: Executes `dotnet test` with coverage collection
3. **Generates HTML report**: Uses ReportGenerator to create a comprehensive coverage report

## Output

The coverage report is generated in the `coveragereport` directory:
- Main report: `coveragereport/index.html`

## Known Warnings

You may see one warning about a generated file:
```
File 'src\Relay\obj\Debug\net8.0\Relay.SourceGenerator\...\RelayRegistration.g.cs' does not exist (any more).
```

This is normal and harmless. It occurs because source-generated files in the `obj` directory are transient and may be cleaned between build steps. The coverage data for these files is still included in the report.

## Manual Commands

If you prefer to run the commands manually:

```powershell
# Clean old results
Remove-Item -Path "TestResults","tests/*/TestResults","tools/*/TestResults" -Recurse -Force -ErrorAction SilentlyContinue

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Generate report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

## Requirements

- .NET 8.0 SDK
- ReportGenerator tool (`dotnet tool install -g dotnet-reportgenerator-globaltool`)
- Coverlet.collector package (already configured in test projects)
