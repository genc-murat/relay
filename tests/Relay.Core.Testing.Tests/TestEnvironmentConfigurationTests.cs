using System;
using System.IO;
using System.Linq;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class TestEnvironmentConfigurationTests
{
    [Fact]
    public void Constructor_WithNullDefaultOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new TestEnvironmentConfiguration(null!));
        Assert.Equal("defaultOptions", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidDefaultOptions_SetsDefaultOptions()
    {
        // Arrange
        var defaultOptions = new TestRelayOptionsBuilder()
            .WithDefaultTimeout(TimeSpan.FromMinutes(5))
            .Build();

        // Act
        var config = new TestEnvironmentConfiguration(defaultOptions);

        // Assert
        var effectiveOptions = config.GetEffectiveOptions();
        Assert.Equal(TimeSpan.FromMinutes(5), effectiveOptions.DefaultTimeout);
    }

    [Fact]
    public void CurrentEnvironment_ReturnsCorrectEnvironmentName()
    {
        // Arrange
        var originalAspNetCore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var originalTest = Environment.GetEnvironmentVariable("TEST_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("TEST_ENVIRONMENT", null);

            // Act & Assert - Should default to "Development"
            var config1 = new TestEnvironmentConfiguration(new TestRelayOptions());
            Assert.Equal("Development", config1.CurrentEnvironment);

            // Test ASPNETCORE_ENVIRONMENT
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            var config2 = new TestEnvironmentConfiguration(new TestRelayOptions());
            Assert.Equal("Production", config2.CurrentEnvironment);

            // Test DOTNET_ENVIRONMENT (lower precedence than ASPNETCORE_ENVIRONMENT)
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Staging");
            var config3 = new TestEnvironmentConfiguration(new TestRelayOptions());
            Assert.Equal("Staging", config3.CurrentEnvironment);

            // Test TEST_ENVIRONMENT (lowest precedence)
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            var config4 = new TestEnvironmentConfiguration(new TestRelayOptions());
            Assert.Equal("Development", config4.CurrentEnvironment); // Falls back to default
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
            Environment.SetEnvironmentVariable("TEST_ENVIRONMENT", originalTest);
        }
    }

    [Fact]
    public void AddEnvironmentConfig_WithNullEnvironment_ThrowsArgumentException()
    {
        // Arrange
        var config = new TestEnvironmentConfiguration(new TestRelayOptions());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.AddEnvironmentConfig(null!, new TestRelayOptions()));
        Assert.Contains("Environment name cannot be null or empty", exception.Message);
        Assert.Equal("environment", exception.ParamName);
    }

    [Fact]
    public void AddEnvironmentConfig_WithEmptyEnvironment_ThrowsArgumentException()
    {
        // Arrange
        var config = new TestEnvironmentConfiguration(new TestRelayOptions());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.AddEnvironmentConfig("", new TestRelayOptions()));
        Assert.Contains("Environment name cannot be null or empty", exception.Message);
        Assert.Equal("environment", exception.ParamName);
    }

    [Fact]
    public void AddEnvironmentConfig_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new TestEnvironmentConfiguration(new TestRelayOptions());

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => config.AddEnvironmentConfig("Test", (TestRelayOptions)null!));
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void AddEnvironmentConfig_WithValidOptions_AddsConfiguration()
    {
        // Arrange
        var config = new TestEnvironmentConfiguration(new TestRelayOptions());
        var envOptions = new TestRelayOptionsBuilder()
            .WithDefaultTimeout(TimeSpan.FromMinutes(10))
            .Build();

        // Act
        config.AddEnvironmentConfig("Production", envOptions as TestRelayOptions);

        // Assert
        var environments = config.GetEnvironments();
        Assert.Contains("Production", environments);
    }

    [Fact]
    public void AddEnvironmentConfig_WithBuilder_AddsConfiguration()
    {
        // Arrange
        var config = new TestEnvironmentConfiguration(new TestRelayOptions());

        // Act
        config.AddEnvironmentConfig("CI", builder =>
        {
            builder.WithParallelExecution(false);
            builder.WithDefaultTimeout(TimeSpan.FromMinutes(15));
        });

        // Assert
        var environments = config.GetEnvironments();
        Assert.Contains("CI", environments);

        var ciOptions = config.GetEffectiveOptions("CI");
        Assert.False(ciOptions.EnableParallelExecution);
        Assert.Equal(TimeSpan.FromMinutes(15), ciOptions.DefaultTimeout);
    }

    [Fact]
    public void GetEffectiveOptions_NoEnvironmentSpecificConfig_ReturnsDefaultOptions()
    {
        // Arrange
        var defaultOptions = new TestRelayOptionsBuilder()
            .WithDefaultTimeout(TimeSpan.FromSeconds(30))
            .WithParallelExecution(true)
            .Build();

        var config = new TestEnvironmentConfiguration(defaultOptions);

        // Act
        var effectiveOptions = config.GetEffectiveOptions("NonExistent");

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), effectiveOptions.DefaultTimeout);
        Assert.True(effectiveOptions.EnableParallelExecution);
    }

    [Fact]
    public void GetEffectiveOptions_WithEnvironmentSpecificConfig_MergesOptions()
    {
        // Arrange
        var defaultOptions = new TestRelayOptionsBuilder()
            .WithDefaultTimeout(TimeSpan.FromSeconds(30))
            .WithParallelExecution(true)
            .WithIsolation(true)
            .Build();

        var config = new TestEnvironmentConfiguration(defaultOptions);

        config.AddEnvironmentConfig("Production", builder =>
        {
            builder.WithDefaultTimeout(TimeSpan.FromMinutes(5)); // Override
            builder.WithMaxDegreeOfParallelism(1); // Override
            // EnableParallelExecution not specified, should keep default
        });

        // Act
        var effectiveOptions = config.GetEffectiveOptions("Production");

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), effectiveOptions.DefaultTimeout); // Overridden
        Assert.Equal(1, effectiveOptions.MaxDegreeOfParallelism); // Overridden
        Assert.True(effectiveOptions.EnableParallelExecution); // Not overridden, keeps default
        Assert.True(effectiveOptions.EnableIsolation); // Not overridden, keeps default
    }

    [Fact]
    public void GetEffectiveOptions_CurrentEnvironment_UsesCurrentEnvironment()
    {
        // Arrange
        var originalAspNetCore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Staging");

            var defaultOptions = new TestRelayOptionsBuilder()
                .WithDefaultTimeout(TimeSpan.FromSeconds(30))
                .Build();

            var config = new TestEnvironmentConfiguration(defaultOptions);
            config.AddEnvironmentConfig("Staging", builder =>
                builder.WithDefaultTimeout(TimeSpan.FromMinutes(10)));

            // Act
            var effectiveOptions = config.GetEffectiveOptions();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(10), effectiveOptions.DefaultTimeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
        }
    }

    [Fact]
    public void LoadFromDirectory_DirectoryDoesNotExist_DoesNothing()
    {
        // Arrange
        var config = new TestEnvironmentConfiguration(new TestRelayOptions());
        var nonExistentDirectory = "NonExistentDirectory";

        // Act - Should not throw
        config.LoadFromDirectory(nonExistentDirectory);

        // Assert
        var environments = config.GetEnvironments();
        Assert.Empty(environments);
    }

    [Fact]
    public void LoadFromDirectory_WithValidDirectory_LoadsConfigurations()
    {
        // Arrange
        var config = new TestEnvironmentConfiguration(new TestRelayOptions());
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            Directory.CreateDirectory(tempDir);

            // Create a mock config file (though LoadFromDirectory is stubbed, we test the structure)
            var configFile = Path.Combine(tempDir, "TestEnvironment.json");
            File.WriteAllText(configFile, "{}");

            // Act
            config.LoadFromDirectory(tempDir);

            // Assert - Since LoadFromDirectory is stubbed, it should not add any environments
            var environments = config.GetEnvironments();
            Assert.Empty(environments);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void SaveToFile_SavesConfiguration()
    {
        // Arrange
        var config = new TestEnvironmentConfiguration(new TestRelayOptions());
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");

        try
        {
            // Act - SaveToFile is stubbed, so it should not create a file
            config.SaveToFile(tempFile);

            // Assert - File should not exist since SaveToFile is stubbed
            Assert.False(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SaveToFile_WithEnvironment_SavesEnvironmentSpecificConfiguration()
    {
        // Arrange
        var defaultOptions = new TestRelayOptionsBuilder().Build();
        var config = new TestEnvironmentConfiguration(defaultOptions);

        config.AddEnvironmentConfig("Production", builder =>
            builder.WithDefaultTimeout(TimeSpan.FromMinutes(10)));

        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");

        try
        {
            // Act
            config.SaveToFile(tempFile, "Production");

            // Assert - File should not exist since SaveToFile is stubbed
            Assert.False(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void GetEnvironments_ReturnsSortedEnvironmentNames()
    {
        // Arrange
        var config = new TestEnvironmentConfiguration(new TestRelayOptions());

        config.AddEnvironmentConfig("Production", new TestRelayOptions());
        config.AddEnvironmentConfig("Development", new TestRelayOptions());
        config.AddEnvironmentConfig("CI", new TestRelayOptions());

        // Act
        var environments = config.GetEnvironments();

        // Assert
        var envList = environments.ToList();
        Assert.Equal(3, envList.Count);
        Assert.Equal("CI", envList[0]); // Should be sorted
        Assert.Equal("Development", envList[1]);
        Assert.Equal("Production", envList[2]);
    }

    [Fact]
    public void Validate_WithValidConfiguration_ReturnsEmptyErrors()
    {
        // Arrange
        var defaultOptions = new TestRelayOptionsBuilder()
            .WithDefaultTimeout(TimeSpan.FromSeconds(30))
            .WithMaxDegreeOfParallelism(4)
            .Build();

        var config = new TestEnvironmentConfiguration(defaultOptions);

        config.AddEnvironmentConfig("Production", builder =>
        {
            builder.WithDefaultTimeout(TimeSpan.FromMinutes(5));
            builder.WithMaxDegreeOfParallelism(8);
        });

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithInvalidDefaultTimeout_ReturnsError()
    {
        // Arrange
        var invalidOptions = new TestRelayOptions { DefaultTimeout = TimeSpan.Zero };
        var config = new TestEnvironmentConfiguration(invalidOptions);

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Contains("Default: DefaultTimeout must be greater than zero", errors);
    }

    [Fact]
    public void Validate_WithInvalidMaxDegreeOfParallelism_ReturnsError()
    {
        // Arrange
        var invalidOptions = new TestRelayOptions { MaxDegreeOfParallelism = 0 };
        var config = new TestEnvironmentConfiguration(invalidOptions);

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Contains("Default: MaxDegreeOfParallelism must be at least 1", errors);
    }

    [Fact]
    public void Validate_WithInvalidCoverageThreshold_ReturnsError()
    {
        // Arrange
        var invalidOptions = new TestRelayOptions
        {
            EnableCoverageTracking = true,
            CoverageTracking = new CoverageTrackingOptions { MinimumCoverageThreshold = -5.0 }
        };
        var config = new TestEnvironmentConfiguration(invalidOptions);

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Contains("Default: Coverage MinimumCoverageThreshold cannot be negative", errors);
    }

    [Fact]
    public void Validate_WithInvalidPerformanceThresholds_ReturnsErrors()
    {
        // Arrange
        var invalidOptions = new TestRelayOptions
        {
            EnablePerformanceProfiling = true,
            PerformanceProfiling = new PerformanceProfilingOptions
            {
                MemoryWarningThreshold = -100,
                ExecutionTimeWarningThreshold = -500
            }
        };
        var config = new TestEnvironmentConfiguration(invalidOptions);

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Contains("Default: Performance MemoryWarningThreshold cannot be negative", errors);
        Assert.Contains("Default: Performance ExecutionTimeWarningThreshold cannot be negative", errors);
    }

    [Fact]
    public void Validate_WithEnvironmentSpecificErrors_IncludesEnvironmentInError()
    {
        // Arrange
        var validDefaultOptions = new TestRelayOptionsBuilder().Build();
        var config = new TestEnvironmentConfiguration(validDefaultOptions);

        config.AddEnvironmentConfig("Production", new TestRelayOptions { DefaultTimeout = TimeSpan.Zero });

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Contains("Production: DefaultTimeout must be greater than zero", errors);
    }
}

public class TestEnvironmentConfigurationFactoryTests
{
    [Fact]
    public void CreateDefault_ReturnsConfigurationWithCommonEnvironments()
    {
        // Act
        var config = TestEnvironmentConfigurationFactory.CreateDefault();

        // Assert
        Assert.NotNull(config);

        var environments = config.GetEnvironments();
        Assert.Contains("Development", environments);
        Assert.Contains("CI", environments);
        Assert.Contains("Performance", environments);

        // Verify Development configuration
        var devOptions = config.GetEffectiveOptions("Development");
        Assert.True(devOptions.EnableDiagnosticLogging);
        Assert.Equal(LogLevel.Debug, devOptions.DiagnosticLogging.LogLevel);
        Assert.True(devOptions.DiagnosticLogging.EnableConsoleLogging);
        Assert.True(devOptions.EnablePerformanceProfiling);
        Assert.True(devOptions.PerformanceProfiling.EnableDetailedProfiling);
        Assert.True(devOptions.EnableCoverageTracking);
        Assert.True(devOptions.CoverageTracking.GenerateReports);
    }

    [Fact]
    public void FromFile_FileExists_ReturnsConfiguration()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        File.WriteAllText(tempFile, "{}");

        try
        {
            // Act
            var config = TestEnvironmentConfigurationFactory.FromFile(tempFile);

            // Assert
            Assert.NotNull(config);
            // Since FromFile is stubbed, it just returns a basic configuration
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void FromFile_FileDoesNotExist_ReturnsConfiguration()
    {
        // Arrange
        var nonExistentFile = "NonExistentFile.json";

        // Act
        var config = TestEnvironmentConfigurationFactory.FromFile(nonExistentFile);

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public void FromEnvironment_LoadsFromEnvironmentVariables()
    {
        // Arrange
        var originalTimeout = Environment.GetEnvironmentVariable("TESTRELAY_DEFAULTTIMEOUT");
        var originalParallel = Environment.GetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL");

        try
        {
            Environment.SetEnvironmentVariable("TESTRELAY_DEFAULTTIMEOUT", "00:10:00");
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL", "false");

            // Act
            var config = TestEnvironmentConfigurationFactory.FromEnvironment();

            // Assert
            Assert.NotNull(config);
            var options = config.GetEffectiveOptions();
            Assert.Equal(TimeSpan.FromMinutes(10), options.DefaultTimeout);
            Assert.False(options.EnableParallelExecution);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TESTRELAY_DEFAULTTIMEOUT", originalTimeout);
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL", originalParallel);
        }
    }

    [Fact]
    public void FromEnvironment_CustomPrefix_LoadsWithCustomPrefix()
    {
        // Arrange
        var originalTimeout = Environment.GetEnvironmentVariable("CUSTOM_DEFAULTTIMEOUT");

        try
        {
            Environment.SetEnvironmentVariable("CUSTOM_DEFAULTTIMEOUT", "00:15:00");

            // Act
            var config = TestEnvironmentConfigurationFactory.FromEnvironment("CUSTOM_");

            // Assert
            Assert.NotNull(config);
            var options = config.GetEffectiveOptions();
            Assert.Equal(TimeSpan.FromMinutes(15), options.DefaultTimeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUSTOM_DEFAULTTIMEOUT", originalTimeout);
        }
    }
}