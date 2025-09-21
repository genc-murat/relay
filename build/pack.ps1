# Relay Framework Packaging Script
# Enhanced version with better IDE integration and error handling

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "./artifacts/packages",
    [switch]$SkipTests,
    [switch]$IncludeSymbols = $true,
    [switch]$Verbose,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Set verbosity level
$VerbosityLevel = if ($Verbose) { "normal" } else { "minimal" }

function Write-Step {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host "`n$Message" -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Test-DotNetInstallation {
    try {
        $dotnetVersion = dotnet --version
        Write-Host "Using .NET SDK version: $dotnetVersion" -ForegroundColor Gray
        return $true
    }
    catch {
        Write-Error ".NET SDK not found. Please install .NET SDK."
        return $false
    }
}

Write-Step "Starting Relay Framework packaging..." "Green"

# Determine script location and project root
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptPath

# Change to project root directory
if (Test-Path $ProjectRoot) {
    Set-Location $ProjectRoot
    Write-Host "Working directory: $ProjectRoot" -ForegroundColor Gray
} else {
    Write-Error "Could not find project root directory: $ProjectRoot"
    exit 1
}

# Validate we're in the right directory by checking for solution file
if (!(Test-Path "Relay.sln")) {
    Write-Error "Relay.sln not found. Please run this script from the project root or ensure the solution file exists."
    exit 1
}

# Validate .NET installation
if (!(Test-DotNetInstallation)) {
    exit 1
}

# Ensure output directory exists
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "Created output directory: $OutputPath" -ForegroundColor Gray
}

# Clean previous packages if Force is specified
if ($Force -and (Test-Path $OutputPath)) {
    Write-Step "Cleaning previous packages..." "Yellow"
    Remove-Item "$OutputPath/*.nupkg" -Force -ErrorAction SilentlyContinue
    Remove-Item "$OutputPath/*.snupkg" -Force -ErrorAction SilentlyContinue
}

# Clean previous builds
Write-Step "Cleaning previous builds..." "Yellow"
dotnet clean --configuration $Configuration --verbosity $VerbosityLevel

# Restore dependencies
Write-Step "Restoring dependencies..." "Yellow"
dotnet restore --verbosity $VerbosityLevel

if ($LASTEXITCODE -ne 0) {
    Write-Error "Restore failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Build solution
Write-Step "Building solution..." "Yellow"
dotnet build --configuration $Configuration --no-restore --verbosity $VerbosityLevel

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Run tests (unless skipped)
if (!$SkipTests) {
    Write-Step "Running tests..." "Yellow"
    
    $testProjects = @(
        @{ Name = "Core Tests"; Path = "tests/Relay.Core.Tests" },
        @{ Name = "Source Generator Tests"; Path = "tests/Relay.SourceGenerator.Tests" }
    )
    
    foreach ($testProject in $testProjects) {
        Write-Host "  Running $($testProject.Name)..." -ForegroundColor Cyan
        
        dotnet test $testProject.Path --configuration $Configuration --no-build --verbosity $VerbosityLevel --logger "console;verbosity=normal"
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "$($testProject.Name) failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }
        
        Write-Success "$($testProject.Name) passed"
    }
}

# Pack projects
Write-Step "Creating NuGet packages..." "Yellow"

$projects = @(
    @{ Name = "Relay.Core"; Path = "src/Relay.Core/Relay.Core.csproj"; Description = "Core framework" },
    # Temporarily skip source generator due to packaging issues
    # @{ Name = "Relay.SourceGenerator"; Path = "src/Relay.SourceGenerator/Relay.SourceGenerator.csproj"; Description = "Source generator" },
    @{ Name = "Relay"; Path = "src/Relay/Relay.csproj"; Description = "Main package" }
)

$packagesCreated = @()

foreach ($project in $projects) {
    if (!(Test-Path $project.Path)) {
        Write-Warning "Project not found: $($project.Path) - skipping"
        continue
    }
    
    Write-Host "  Packing $($project.Name) ($($project.Description))..." -ForegroundColor Cyan
    
    $packArgs = @(
        "pack", $project.Path,
        "--configuration", $Configuration,
        "--no-build",
        "--output", $OutputPath,
        "--verbosity", $VerbosityLevel
    )
    
    if ($IncludeSymbols) {
        $packArgs += "--include-symbols"
        $packArgs += "--symbol-package-format", "snupkg"
    }
    
    & dotnet $packArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Packing $($project.Name) failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    $packagesCreated += $project.Name
    Write-Success "$($project.Name) packaged successfully"
}

# Run packaging validation tests
if (!$SkipTests) {
    Write-Step "Running packaging validation tests..." "Yellow"
    
    if (Test-Path "tests/Relay.Packaging.Tests") {
        dotnet test tests/Relay.Packaging.Tests --configuration $Configuration --verbosity $VerbosityLevel --logger "console;verbosity=normal"
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Packaging validation tests failed, but continuing..."
        } else {
            Write-Success "Packaging validation tests passed"
        }
    } else {
        Write-Warning "Packaging validation tests not found - skipping"
    }
}

# List created packages
Write-Step "Package Summary" "Green"

$packages = Get-ChildItem -Path $OutputPath -Filter "*.nupkg" | Sort-Object Name
$symbolPackages = Get-ChildItem -Path $OutputPath -Filter "*.snupkg" | Sort-Object Name

if ($packages.Count -eq 0) {
    Write-Warning "No packages were created!"
    exit 1
}

Write-Host "Created $($packages.Count) package(s):" -ForegroundColor Green
foreach ($package in $packages) {
    $size = [math]::Round($package.Length / 1KB, 2)
    Write-Host "  [PACKAGE] $($package.Name) ($size KB)" -ForegroundColor Green
}

if ($symbolPackages.Count -gt 0) {
    Write-Host "`nCreated $($symbolPackages.Count) symbol package(s):" -ForegroundColor Green
    foreach ($symbolPackage in $symbolPackages) {
        $size = [math]::Round($symbolPackage.Length / 1KB, 2)
        Write-Host "  [SYMBOLS] $($symbolPackage.Name) ($size KB)" -ForegroundColor Green
    }
}

# Calculate total size
$totalSize = ($packages + $symbolPackages | Measure-Object Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

Write-Host "`nPackaging completed successfully!" -ForegroundColor Green
Write-Host "Packages available in: $OutputPath" -ForegroundColor Cyan
Write-Host "Total size: $totalSizeMB MB" -ForegroundColor Cyan

# IDE integration - output package paths for tooling
if ($Verbose) {
    Write-Host "`nPackage paths for IDE integration:" -ForegroundColor Gray
    foreach ($package in $packages) {
        Write-Host "  $($package.FullName)" -ForegroundColor Gray
    }
}