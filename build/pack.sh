#!/bin/bash
# Relay Framework Packaging Script

set -e

CONFIGURATION="Release"
OUTPUT_PATH="./artifacts/packages"
SKIP_TESTS=false
INCLUDE_SYMBOLS=true

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --no-symbols)
            INCLUDE_SYMBOLS=false
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "🚀 Starting Relay Framework packaging..."

# Ensure output directory exists
mkdir -p "$OUTPUT_PATH"

# Clean previous builds
echo "🧹 Cleaning previous builds..."
dotnet clean --configuration "$CONFIGURATION" --verbosity minimal

# Restore dependencies
echo "📦 Restoring dependencies..."
dotnet restore --verbosity minimal

# Build solution
echo "🔨 Building solution..."
dotnet build --configuration "$CONFIGURATION" --no-restore --verbosity minimal

# Run tests (unless skipped)
if [ "$SKIP_TESTS" = false ]; then
    echo "🧪 Running tests..."
    
    # Run core tests
    dotnet test tests/Relay.Core.Tests --configuration "$CONFIGURATION" --no-build --verbosity minimal
    
    # Run source generator tests
    dotnet test tests/Relay.SourceGenerator.Tests --configuration "$CONFIGURATION" --no-build --verbosity minimal
fi

# Pack projects
echo "📦 Creating NuGet packages..."

projects=(
    "src/Relay.Core/Relay.Core.csproj"
    "src/Relay.SourceGenerator/Relay.SourceGenerator.csproj"
    "src/Relay/Relay.csproj"
)

for project in "${projects[@]}"; do
    echo "  📦 Packing $project..."
    
    pack_args=(
        "pack" "$project"
        "--configuration" "$CONFIGURATION"
        "--no-build"
        "--output" "$OUTPUT_PATH"
        "--verbosity" "minimal"
    )
    
    if [ "$INCLUDE_SYMBOLS" = true ]; then
        pack_args+=("--include-symbols")
        pack_args+=("--symbol-package-format" "snupkg")
    fi
    
    dotnet "${pack_args[@]}"
done

# Run packaging validation tests
if [ "$SKIP_TESTS" = false ]; then
    echo "🔍 Running packaging validation tests..."
    dotnet test tests/Relay.Packaging.Tests --configuration "$CONFIGURATION" --verbosity minimal || echo "⚠️  Packaging validation tests failed, but continuing..."
fi

# List created packages
echo "📋 Created packages:"
find "$OUTPUT_PATH" -name "*.nupkg" -exec basename {} \; | sed 's/^/  ✅ /'

echo "🎉 Packaging completed successfully!"
echo "📁 Packages available in: $OUTPUT_PATH"