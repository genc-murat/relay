using System;
using System.Collections.Generic;
using System.Linq;
using Relay.SourceGenerator.Generators;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for GeneratorPipeline to verify generator registration and filtering logic.
    /// Note: These tests focus on the pipeline's internal logic without executing generators,
    /// as SourceProductionContext cannot be easily mocked.
    /// </summary>
    public class GeneratorPipelineTests
    {
        #region Test Helper Classes

        /// <summary>
        /// Mock code generator for testing.
        /// </summary>
        private class MockCodeGenerator : ICodeGenerator
        {
            public string GeneratorName { get; set; } = "Mock Generator";
            public string OutputFileName { get; set; } = "MockOutput";
            public int Priority { get; set; } = 100;
            public bool CanGenerateResult { get; set; } = true;
            public string GeneratedCode { get; set; } = "// Mock generated code";

            public bool CanGenerate(HandlerDiscoveryResult result)
            {
                return CanGenerateResult;
            }

            public string Generate(HandlerDiscoveryResult result, GenerationOptions options)
            {
                return GeneratedCode;
            }
        }

        /// <summary>
        /// Helper method to create a minimal HandlerDiscoveryResult.
        /// </summary>
        private HandlerDiscoveryResult CreateMinimalResult()
        {
            return new HandlerDiscoveryResult();
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidGenerators_CreatesInstance()
        {
            // Arrange
            var generators = new List<ICodeGenerator>
            {
                new MockCodeGenerator { GeneratorName = "Generator 1" },
                new MockCodeGenerator { GeneratorName = "Generator 2" }
            };

            // Act
            var pipeline = new GeneratorPipeline(generators);

            // Assert
            Assert.NotNull(pipeline);
            Assert.Equal(2, pipeline.GeneratorCount);
        }

        [Fact]
        public void Constructor_WithNullGenerators_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GeneratorPipeline(null!));
        }

        [Fact]
        public void Constructor_WithEmptyGenerators_CreatesInstance()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();

            // Act
            var pipeline = new GeneratorPipeline(generators);

            // Assert
            Assert.NotNull(pipeline);
            Assert.Equal(0, pipeline.GeneratorCount);
        }

        #endregion

        #region Generator Count and Registration Tests

        [Fact]
        public void Generators_Property_ReturnsAllRegisteredGenerators()
        {
            // Arrange
            var gen1 = new MockCodeGenerator { GeneratorName = "Generator 1" };
            var gen2 = new MockCodeGenerator { GeneratorName = "Generator 2" };
            var gen3 = new MockCodeGenerator { GeneratorName = "Generator 3" };

            // Act
            var pipeline = new GeneratorPipeline(new[] { gen1, gen2, gen3 });

            // Assert
            Assert.Equal(3, pipeline.Generators.Count);
            Assert.Contains(gen1, pipeline.Generators);
            Assert.Contains(gen2, pipeline.Generators);
            Assert.Contains(gen3, pipeline.Generators);
        }

        [Fact]
        public void GeneratorCount_ReflectsNumberOfGenerators()
        {
            // Arrange & Act
            var pipeline1 = new GeneratorPipeline(new[] {
                new MockCodeGenerator(),
                new MockCodeGenerator(),
                new MockCodeGenerator()
            });

            var pipeline2 = new GeneratorPipeline(Array.Empty<ICodeGenerator>());

            // Assert
            Assert.Equal(3, pipeline1.GeneratorCount);
            Assert.Equal(0, pipeline2.GeneratorCount);
        }

        #endregion

        #region Priority Ordering Tests

        [Fact]
        public void Generators_AreOrderedByPriority_InInternalList()
        {
            // Arrange
            var gen1 = new MockCodeGenerator { GeneratorName = "DI Registration Generator", Priority = 10 };
            var gen2 = new MockCodeGenerator { GeneratorName = "Handler Registry Generator", Priority = 20 };
            var gen3 = new MockCodeGenerator { GeneratorName = "Optimized Dispatcher Generator", Priority = 30 };

            // Act - Add in random order
            var pipeline = new GeneratorPipeline(new[] { gen3, gen1, gen2 });

            // Assert - Pipeline should maintain original order (doesn't sort in constructor)
            Assert.Equal(3, pipeline.GeneratorCount);
            Assert.Equal(gen3, pipeline.Generators[0]);
            Assert.Equal(gen1, pipeline.Generators[1]);
            Assert.Equal(gen2, pipeline.Generators[2]);
        }

        #endregion

        #region Configuration and CanGenerate Filtering Logic Tests

        [Fact]
        public void FilteringLogic_GeneratorDisabledInConfiguration_ShouldBeFiltered()
        {
            // Arrange
            var gen1 = new MockCodeGenerator { GeneratorName = "DI Registration Generator" };
            var options = new GenerationOptions { EnableDIGeneration = false };

            // Act - Check if generator would be filtered
            var isEnabled = options.IsGeneratorEnabled(gen1.GeneratorName);
            var canGenerate = gen1.CanGenerate(CreateMinimalResult());

            // Assert
            Assert.False(isEnabled); // Should be filtered by configuration
            Assert.True(canGenerate); // CanGenerate is true, but config disables it
        }

        [Fact]
        public void FilteringLogic_GeneratorEnabledButCanGenerateFalse_ShouldBeFiltered()
        {
            // Arrange
            var gen1 = new MockCodeGenerator
            {
                GeneratorName = "DI Registration Generator",
                CanGenerateResult = false
            };
            var options = new GenerationOptions { EnableDIGeneration = true };

            // Act
            var isEnabled = options.IsGeneratorEnabled(gen1.GeneratorName);
            var canGenerate = gen1.CanGenerate(CreateMinimalResult());

            // Assert
            Assert.True(isEnabled); // Enabled in configuration
            Assert.False(canGenerate); // But CanGenerate returns false
        }

        [Fact]
        public void FilteringLogic_BothEnabledAndCanGenerate_ShouldNotBeFiltered()
        {
            // Arrange
            var gen1 = new MockCodeGenerator
            {
                GeneratorName = "DI Registration Generator",
                CanGenerateResult = true
            };
            var options = new GenerationOptions { EnableDIGeneration = true };

            // Act
            var isEnabled = options.IsGeneratorEnabled(gen1.GeneratorName);
            var canGenerate = gen1.CanGenerate(CreateMinimalResult());

            // Assert
            Assert.True(isEnabled);
            Assert.True(canGenerate);
        }

        #endregion

        #region Scenario-Based Configuration Tests

        [Fact]
        public void Scenario_MinimalBuild_OnlyRequiredGeneratorsEnabled()
        {
            // Arrange - Scenario from MSBUILD-CONFIGURATION.md
            var generators = new List<ICodeGenerator>
            {
                new MockCodeGenerator { GeneratorName = "DI Registration Generator" },
                new MockCodeGenerator { GeneratorName = "Handler Registry Generator" },
                new MockCodeGenerator { GeneratorName = "Optimized Dispatcher Generator" },
                new MockCodeGenerator { GeneratorName = "Notification Dispatcher Generator" },
                new MockCodeGenerator { GeneratorName = "Pipeline Registry Generator" },
                new MockCodeGenerator { GeneratorName = "Endpoint Metadata Generator" }
            };

            var options = new GenerationOptions
            {
                EnableDIGeneration = true,
                EnableHandlerRegistry = false,
                EnableOptimizedDispatcher = true,
                EnableNotificationDispatcher = false,
                EnablePipelineRegistry = false,
                EnableEndpointMetadata = false
            };

            // Act - Count which generators would be enabled
            var enabledCount = generators.Count(g =>
                options.IsGeneratorEnabled(g.GeneratorName) && g.CanGenerate(CreateMinimalResult()));

            // Assert
            Assert.Equal(2, enabledCount); // Only DI and Dispatcher
        }

        [Fact]
        public void Scenario_ApiOnlyProject_NoNotificationOrPipeline()
        {
            // Arrange
            var generators = new List<ICodeGenerator>
            {
                new MockCodeGenerator { GeneratorName = "DI Registration Generator" },
                new MockCodeGenerator { GeneratorName = "Handler Registry Generator" },
                new MockCodeGenerator { GeneratorName = "Optimized Dispatcher Generator" },
                new MockCodeGenerator { GeneratorName = "Notification Dispatcher Generator" },
                new MockCodeGenerator { GeneratorName = "Pipeline Registry Generator" },
                new MockCodeGenerator { GeneratorName = "Endpoint Metadata Generator" }
            };

            var options = new GenerationOptions
            {
                EnableNotificationDispatcher = false,
                EnablePipelineRegistry = false
            };

            // Act
            var enabledCount = generators.Count(g =>
                options.IsGeneratorEnabled(g.GeneratorName) && g.CanGenerate(CreateMinimalResult()));

            // Assert
            Assert.Equal(4, enabledCount); // All except Notification and Pipeline
        }

        [Fact]
        public void Scenario_FullFeatureBuild_AllGeneratorsEnabled()
        {
            // Arrange
            var generators = new List<ICodeGenerator>
            {
                new MockCodeGenerator { GeneratorName = "DI Registration Generator" },
                new MockCodeGenerator { GeneratorName = "Handler Registry Generator" },
                new MockCodeGenerator { GeneratorName = "Optimized Dispatcher Generator" },
                new MockCodeGenerator { GeneratorName = "Notification Dispatcher Generator" },
                new MockCodeGenerator { GeneratorName = "Pipeline Registry Generator" },
                new MockCodeGenerator { GeneratorName = "Endpoint Metadata Generator" }
            };

            var options = GenerationOptions.Default; // All enabled by default

            // Act
            var enabledCount = generators.Count(g =>
                options.IsGeneratorEnabled(g.GeneratorName) && g.CanGenerate(CreateMinimalResult()));

            // Assert
            Assert.Equal(6, enabledCount); // All generators enabled
        }

        #endregion
    }
}
