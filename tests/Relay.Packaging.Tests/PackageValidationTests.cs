using System.IO.Compression;
using System.Linq;
using NuGet.Packaging;
using Xunit;

namespace Relay.Packaging.Tests;

public class PackageValidationTests
{
    private static readonly string PackageOutputBase = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../../src"));

    [Theory]
    [InlineData("Relay")]
    [InlineData("Relay.Core")]
    [InlineData("Relay.SourceGenerator")]
    public void Package_ShouldExist(string packageName)
    {
        var packagePath = GetPackagePath(packageName);
        Assert.True(File.Exists(packagePath), $"Package {packageName} should exist at {packagePath}");
    }

    [Theory]
    [InlineData("Relay")]
    [InlineData("Relay.Core")]
    [InlineData("Relay.SourceGenerator")]
    public void Package_ShouldHaveValidMetadata(string packageName)
    {
        var packagePath = GetPackagePath(packageName);
        using var packageReader = new PackageArchiveReader(packagePath);
        var nuspec = packageReader.NuspecReader;

        Assert.NotNull(nuspec.GetId());
        Assert.NotNull(nuspec.GetVersion());
        Assert.NotNull(nuspec.GetAuthors());
        Assert.NotNull(nuspec.GetDescription());
        Assert.NotNull(nuspec.GetProjectUrl());
        Assert.Equal("MIT", nuspec.GetLicenseMetadata()?.License);
    }

    [Fact]
    public void SourceGeneratorPackage_ShouldIncludeAnalyzerFiles()
    {
        var packagePath = GetPackagePath("Relay.SourceGenerator");
        using var packageReader = new PackageArchiveReader(packagePath);

        // GetAnalyzerItems method not available in this version
        var analyzerItems = packageReader.GetFiles().Where(f => f.StartsWith("analyzers/")).ToList();
        Assert.NotEmpty(analyzerItems);

        var hasSourceGeneratorDll = analyzerItems.Any(file =>
            file.EndsWith("Relay.SourceGenerator.dll"));
        Assert.True(hasSourceGeneratorDll, "Source generator package should include analyzer DLL");
    }

    [Theory]
    [InlineData("Relay")]
    [InlineData("Relay.Core")]
    [InlineData("Relay.SourceGenerator")]
    public void Package_ShouldIncludeSymbols(string packageName)
    {
        var symbolsPackagePath = GetSymbolsPackagePath(packageName);
        Assert.True(File.Exists(symbolsPackagePath),
            $"Symbols package should exist for {packageName} at {symbolsPackagePath}");
    }

    [Theory]
    [InlineData("Relay.Core")]
    public void CorePackage_ShouldHaveCorrectDependencies(string packageName)
    {
        var packagePath = GetPackagePath(packageName);
        using var packageReader = new PackageArchiveReader(packagePath);
        var dependencies = packageReader.NuspecReader.GetDependencyGroups();

        var expectedDependencies = new[]
        {
            "Microsoft.Extensions.Configuration.Abstractions",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Logging.Abstractions",
            "Microsoft.Extensions.ObjectPool",
            "Microsoft.Extensions.Options"
        };

        foreach (var expectedDep in expectedDependencies)
        {
            var hasDependency = dependencies.Any(group =>
                group.Packages.Any(pkg => pkg.Id == expectedDep));
            Assert.True(hasDependency, $"Package should have dependency on {expectedDep}");
        }
    }

    [Fact]
    public void SourceGeneratorPackage_ShouldBeDevelopmentDependency()
    {
        var packagePath = GetPackagePath("Relay.SourceGenerator");
        using var packageReader = new PackageArchiveReader(packagePath);
        var nuspec = packageReader.NuspecReader;

        var developmentDependency = nuspec.GetDevelopmentDependency();
        Assert.True(developmentDependency, "Source generator should be marked as development dependency");
    }

    private static string GetPackagePath(string packageName)
    {
        var searchPattern = $"{packageName}.*.nupkg";
        var baseDir = Path.Combine(PackageOutputBase, packageName, "bin");
        
        // Try different possible locations for the package
        var possibleDirs = new[]
        {
            Path.Combine(baseDir, "Release"),
            Path.Combine(baseDir, "windows", "Release"),
            Path.Combine(baseDir, "Debug"),
            Path.Combine(baseDir, "windows", "Debug"),
            Path.Combine(AppContext.BaseDirectory, "../../../artifacts/packages"),
            Path.Combine(AppContext.BaseDirectory, "../../../artifacts/test"),
            Path.Combine(AppContext.BaseDirectory, "../../../../../../artifacts/packages"),
            Path.Combine(AppContext.BaseDirectory, "../../../../../../artifacts/test")
        };

        // Try to get the project root directory by going up from AppContext.BaseDirectory
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            // Check if we've reached the project root by looking for the solution file
            if (File.Exists(Path.Combine(currentDir.FullName, "Relay.sln")))
            {
                possibleDirs = possibleDirs.Concat(new[]
                {
                    Path.Combine(currentDir.FullName, "artifacts", "packages"),
                    Path.Combine(currentDir.FullName, "artifacts", "test"),
                    Path.Combine(currentDir.FullName, "packoutput")
                }).ToArray();
                break;
            }
            currentDir = currentDir.Parent;
        }

        string? packageFile = null;
        foreach (var packageDir in possibleDirs)
        {
            if (Directory.Exists(packageDir))
            {
                var packageFiles = Directory.GetFiles(packageDir, searchPattern, SearchOption.AllDirectories);
                packageFile = packageFiles.FirstOrDefault(f => !f.Contains(".symbols."));
                if (packageFile != null)
                {
                    break;
                }
            }
        }

        if (packageFile == null)
        {
            throw new FileNotFoundException($"Package file not found matching pattern: {searchPattern} in any of the expected directories");
        }

        return packageFile;
    }

    private static string GetSymbolsPackagePath(string packageName)
    {
        var searchPattern = $"{packageName}.*.symbols.nupkg";
        var baseDir = Path.Combine(PackageOutputBase, packageName, "bin");
        
        // Try different possible locations for the symbols package
        var possibleDirs = new[]
        {
            Path.Combine(baseDir, "Release"),
            Path.Combine(baseDir, "windows", "Release"),
            Path.Combine(baseDir, "Debug"),
            Path.Combine(baseDir, "windows", "Debug"),
            Path.Combine(AppContext.BaseDirectory, "../../../artifacts/packages"),
            Path.Combine(AppContext.BaseDirectory, "../../../artifacts/test"),
            Path.Combine(AppContext.BaseDirectory, "../../../../../../artifacts/packages"),
            Path.Combine(AppContext.BaseDirectory, "../../../../../../artifacts/test")
        };

        // Try to get the project root directory by going up from AppContext.BaseDirectory
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            // Check if we've reached the project root by looking for the solution file
            if (File.Exists(Path.Combine(currentDir.FullName, "Relay.sln")))
            {
                possibleDirs = possibleDirs.Concat(new[]
                {
                    Path.Combine(currentDir.FullName, "artifacts", "packages"),
                    Path.Combine(currentDir.FullName, "artifacts", "test"),
                    Path.Combine(currentDir.FullName, "packoutput")
                }).ToArray();
                break;
            }
            currentDir = currentDir.Parent;
        }

        foreach (var packageDir in possibleDirs)
        {
            if (Directory.Exists(packageDir))
            {
                var packageFiles = Directory.GetFiles(packageDir, searchPattern, SearchOption.AllDirectories);
                var packageFile = packageFiles.FirstOrDefault();
                if (packageFile != null)
                {
        return packageFile!;
                }
            }
        }

        throw new FileNotFoundException($"Symbols package not found: {searchPattern} in any of the expected directories");
    }
}