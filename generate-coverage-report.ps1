#!/usr/bin/env pwsh
# Script to generate a clean code coverage report

param(
    [switch]$SkipTests = $false,
    [string]$ReportType = "Html"
)

Write-Host "=== Code Coverage Report Generator ===" -ForegroundColor Cyan
Write-Host ""

# Clean up old test results
Write-Host "Cleaning up old test results..." -ForegroundColor Yellow
Remove-Item -Path "TestResults" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "tests/*/TestResults" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "tools/*/TestResults" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "✓ Old test results cleaned" -ForegroundColor Green
Write-Host ""

# Run tests with coverage collection (unless skipped)
if (-not $SkipTests) {
    Write-Host "Running tests with coverage collection..." -ForegroundColor Yellow
    $testResult = dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
    $testExitCode = $LASTEXITCODE
    
    if ($testExitCode -ne 0) {
        Write-Host "⚠ Tests completed with failures (exit code: $testExitCode)" -ForegroundColor Yellow
        Write-Host "Continuing with coverage report generation..." -ForegroundColor Yellow
    } else {
        Write-Host "✓ Tests completed successfully" -ForegroundColor Green
    }
    Write-Host ""
}

# Check if coverage files exist
$coverageFiles = Get-ChildItem -Path "." -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
if ($coverageFiles.Count -eq 0) {
    Write-Host "✗ No coverage files found. Please run tests first." -ForegroundColor Red
    Write-Host "  Run: dotnet test --collect:'XPlat Code Coverage' --settings coverlet.runsettings" -ForegroundColor Gray
    exit 1
}

Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Gray
Write-Host ""

# Generate coverage report
Write-Host "Generating coverage report..." -ForegroundColor Yellow
reportgenerator `
    -reports:"**/coverage.cobertura.xml" `
    -targetdir:"coveragereport" `
    -reporttypes:$ReportType

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Coverage report generated successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "Report location: $(Resolve-Path 'coveragereport/index.html')" -ForegroundColor Cyan
} else {
    Write-Host "✗ Failed to generate coverage report" -ForegroundColor Red
    exit $LASTEXITCODE
}
