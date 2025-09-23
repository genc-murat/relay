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
    public void RelayPackage_ShouldIncludeBothDependencies()
    {
    var packagePath = GetPackagePath("Relay");
        using var packageReader = new PackageArchiveReader(packagePath);
        var dependencies = packageReader.NuspecReader.GetDependencyGroups();

        var hasCoreReference = dependencies.Any(group => 
            group.Packages.Any(pkg => pkg.Id == "Relay.Core"));
        var hasSourceGeneratorReference = dependencies.Any(group => 
            group.Packages.Any(pkg => pkg.Id == "Relay.SourceGenerator"));

        Assert.True(hasCoreReference, "Relay package should reference Relay.Core");
        Assert.True(hasSourceGeneratorReference, "Relay package should reference Relay.SourceGenerator");
    }

    [Theory]
    [InlineData("Relay", new[] { "netstandard2.0", "net6.0", "net8.0" })]
    [InlineData("Relay.Core", new[] { "netstandard2.0", "net6.0", "net8.0" })]
    [InlineData("Relay.SourceGenerator", new[] { "netstandard2.0" })]
    public void Package_ShouldSupportExpectedTargetFrameworks(string packageName, string[] expectedFrameworks)
    {
        var packagePath = GetPackagePath(packageName);
        using var packageReader = new PackageArchiveReader(packagePath);
        
        var libItems = packageReader.GetLibItems().ToList();
        var actualFrameworks = libItems.Select(item => item.TargetFramework.GetShortFolderName()).ToArray();

        foreach (var expectedFramework in expectedFrameworks)
        {
            Assert.Contains(expectedFramework, actualFrameworks);
        }
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
        var packageDir = Path.Combine(PackageOutputBase, packageName, "bin", "Release");
        
        if (!Directory.Exists(packageDir))
        {
            throw new DirectoryNotFoundException($"Package directory not found: {packageDir}");
        }

        var packageFiles = Directory.GetFiles(packageDir, searchPattern, SearchOption.AllDirectories);
        var packageFile = packageFiles.FirstOrDefault(f => !f.Contains(".symbols."));
        
        if (packageFile == null)
        {
            throw new FileNotFoundException($"Package file not found matching pattern: {searchPattern} in {packageDir}");
        }

        return packageFile;
    }

    private static string GetSymbolsPackagePath(string packageName)
    {
    var searchPattern = $"{packageName}.*.symbols.nupkg";
    var packageDir = Path.Combine(PackageOutputBase, packageName, "bin", "Release");
        
        var packageFiles = Directory.GetFiles(packageDir, searchPattern, SearchOption.AllDirectories);
        return packageFiles.FirstOrDefault() ?? throw new FileNotFoundException($"Symbols package not found: {searchPattern}");
    }
}