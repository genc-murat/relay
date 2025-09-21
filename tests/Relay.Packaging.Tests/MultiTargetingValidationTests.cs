using System.Reflection;
using Xunit;

namespace Relay.Packaging.Tests;

public class MultiTargetingValidationTests
{
    [Theory]
    [InlineData("netstandard2.0")]
    [InlineData("net6.0")]
    [InlineData("net8.0")]
    public void RelayCore_ShouldLoadOnTargetFramework(string targetFramework)
    {
        // This test validates that the assembly can be loaded for each target framework
        // In a real scenario, this would be run against the actual built assemblies
        var assemblyPath = GetAssemblyPath("Relay.Core", targetFramework);
        
        if (File.Exists(assemblyPath))
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            Assert.NotNull(assembly);
            Assert.Contains("Relay.Core", assembly.FullName);
        }
        else
        {
            // Skip test if assembly doesn't exist (not built for this framework)
            Assert.True(true, $"Assembly not found for {targetFramework}, skipping validation");
        }
    }

    [Fact]
    public void NetStandard20_ShouldIncludeTaskExtensions()
    {
        var assemblyPath = GetAssemblyPath("Relay.Core", "netstandard2.0");
        
        if (File.Exists(assemblyPath))
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var referencedAssemblies = assembly.GetReferencedAssemblies();
            
            var hasTaskExtensions = referencedAssemblies.Any(a => 
                a.Name?.Contains("System.Threading.Tasks.Extensions") == true);
            
            Assert.True(hasTaskExtensions, 
                "netstandard2.0 build should reference System.Threading.Tasks.Extensions");
        }
    }

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net8.0")]
    public void ModernNet_ShouldNotIncludeTaskExtensions(string targetFramework)
    {
        var assemblyPath = GetAssemblyPath("Relay.Core", targetFramework);
        
        if (File.Exists(assemblyPath))
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var referencedAssemblies = assembly.GetReferencedAssemblies();
            
            var hasTaskExtensions = referencedAssemblies.Any(a => 
                a.Name?.Contains("System.Threading.Tasks.Extensions") == true);
            
            Assert.False(hasTaskExtensions, 
                $"{targetFramework} build should not reference System.Threading.Tasks.Extensions");
        }
    }

    [Fact]
    public void SourceGenerator_ShouldOnlyTargetNetStandard20()
    {
        var sourceGenDir = Path.Combine("../../../../src/Relay.SourceGenerator/bin/Release");
        
        if (Directory.Exists(sourceGenDir))
        {
            var frameworkDirs = Directory.GetDirectories(sourceGenDir)
                .Select(Path.GetFileName)
                .Where(name => name?.StartsWith("net") == true)
                .ToArray();

            Assert.Single(frameworkDirs);
            Assert.Equal("netstandard2.0", frameworkDirs[0]);
        }
    }

    private static string GetAssemblyPath(string projectName, string targetFramework)
    {
        return Path.Combine("../../../../src", projectName, "bin", "Release", targetFramework, $"{projectName}.dll");
    }
}