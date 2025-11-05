using System;
using System.Collections.Generic;
using Relay.Core.Diagnostics.Registry;
using Xunit;

namespace Relay.Core.Tests.Diagnostics.Registry
{
    public class HandlerRegistryInfoTests
    {
        [Fact]
        public void Constructor_CreatesInstanceWithDefaultValues()
        {
            // Act
            var registry = new HandlerRegistryInfo();

            // Assert
            Assert.NotNull(registry);
            Assert.IsType<HandlerRegistryInfo>(registry);
        }

        [Fact]
        public void DefaultValues_AreSetCorrectly()
        {
            // Act
            var registry = new HandlerRegistryInfo();

            // Assert
            Assert.Equal(string.Empty, registry.AssemblyName);
            Assert.Equal(default(DateTime), registry.GenerationTime);
            Assert.NotNull(registry.Handlers);
            Assert.Empty(registry.Handlers);
            Assert.NotNull(registry.Pipelines);
            Assert.Empty(registry.Pipelines);
            Assert.NotNull(registry.Warnings);
            Assert.Empty(registry.Warnings);
            Assert.Equal(0, registry.TotalHandlers);
            Assert.Equal(0, registry.TotalPipelines);
        }

        [Fact]
        public void AssemblyName_CanBeSetAndRetrieved()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();

            // Act
            registry.AssemblyName = "TestAssembly";

            // Assert
            Assert.Equal("TestAssembly", registry.AssemblyName);
        }

        [Fact]
        public void GenerationTime_CanBeSetAndRetrieved()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();
            var testTime = new DateTime(2023, 1, 1, 12, 0, 0);

            // Act
            registry.GenerationTime = testTime;

            // Assert
            Assert.Equal(testTime, registry.GenerationTime);
        }

        [Fact]
        public void Handlers_ListCanBeModified()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();
            var handler1 = new HandlerInfo
            {
                RequestType = "Request1",
                HandlerType = "Handler1",
                MethodName = "Handle"
            };
            var handler2 = new HandlerInfo
            {
                RequestType = "Request2",
                HandlerType = "Handler2",
                MethodName = "Handle"
            };

            // Act
            registry.Handlers.Add(handler1);
            registry.Handlers.Add(handler2);

            // Assert
            Assert.Equal(2, registry.Handlers.Count);
            Assert.Contains(handler1, registry.Handlers);
            Assert.Contains(handler2, registry.Handlers);
            Assert.Equal(2, registry.TotalHandlers);
        }

        [Fact]
        public void Pipelines_ListCanBeModified()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();
            var pipeline1 = new PipelineInfo
            {
                PipelineType = "TestPipeline1",
                MethodName = "ExecuteAsync",
                Order = 1,
                Scope = "Global"
            };
            var pipeline2 = new PipelineInfo
            {
                PipelineType = "TestPipeline2",
                MethodName = "ExecuteAsync",
                Order = 2,
                Scope = "Request"
            };

            // Act
            registry.Pipelines.Add(pipeline1);
            registry.Pipelines.Add(pipeline2);

            // Assert
            Assert.Equal(2, registry.Pipelines.Count);
            Assert.Contains(pipeline1, registry.Pipelines);
            Assert.Contains(pipeline2, registry.Pipelines);
            Assert.Equal(2, registry.TotalPipelines);
        }

        [Fact]
        public void Warnings_ListCanBeModified()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();

            // Act
            registry.Warnings.Add("Warning 1");
            registry.Warnings.Add("Warning 2");

            // Assert
            Assert.Equal(2, registry.Warnings.Count);
            Assert.Contains("Warning 1", registry.Warnings);
            Assert.Contains("Warning 2", registry.Warnings);
        }

        [Fact]
        public void TotalHandlers_ReturnsCorrectCount()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();

            // Act
            registry.Handlers.Add(new HandlerInfo());
            registry.Handlers.Add(new HandlerInfo());
            registry.Handlers.Add(new HandlerInfo());

            // Assert
            Assert.Equal(3, registry.TotalHandlers);
        }

        [Fact]
        public void TotalPipelines_ReturnsCorrectCount()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();

            // Act
            registry.Pipelines.Add(new PipelineInfo());
            registry.Pipelines.Add(new PipelineInfo());

            // Assert
            Assert.Equal(2, registry.TotalPipelines);
        }

        [Fact]
        public void CanBeInitializedWithObjectInitializer()
        {
            // Arrange
            var testTime = DateTime.UtcNow;
            var handlers = new List<HandlerInfo>
            {
                new HandlerInfo { RequestType = "TestRequest", HandlerType = "TestHandler" }
            };
            var pipelines = new List<PipelineInfo>
            {
                new PipelineInfo { PipelineType = "TestPipeline", MethodName = "ExecuteAsync" }
            };
            var warnings = new List<string> { "Test warning" };

            // Act
            var registry = new HandlerRegistryInfo
            {
                AssemblyName = "TestAssembly.dll",
                GenerationTime = testTime,
                Handlers = handlers,
                Pipelines = pipelines,
                Warnings = warnings
            };

            // Assert
            Assert.Equal("TestAssembly.dll", registry.AssemblyName);
            Assert.Equal(testTime, registry.GenerationTime);
            Assert.Equal(handlers, registry.Handlers);
            Assert.Equal(pipelines, registry.Pipelines);
            Assert.Equal(warnings, registry.Warnings);
            Assert.Equal(1, registry.TotalHandlers);
            Assert.Equal(1, registry.TotalPipelines);
        }

        [Fact]
        public void HandlersList_IsInitiallyEmpty()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();

            // Assert
            Assert.NotNull(registry.Handlers);
            Assert.Empty(registry.Handlers);
            Assert.Equal(0, registry.TotalHandlers);
        }

        [Fact]
        public void PipelinesList_IsInitiallyEmpty()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();

            // Assert
            Assert.NotNull(registry.Pipelines);
            Assert.Empty(registry.Pipelines);
            Assert.Equal(0, registry.TotalPipelines);
        }

        [Fact]
        public void WarningsList_IsInitiallyEmpty()
        {
            // Arrange
            var registry = new HandlerRegistryInfo();

            // Assert
            Assert.NotNull(registry.Warnings);
            Assert.Empty(registry.Warnings);
        }

        [Fact]
        public void MultipleInstances_HaveIndependentState()
        {
            // Arrange
            var registry1 = new HandlerRegistryInfo
            {
                AssemblyName = "Assembly1",
                Handlers = { new HandlerInfo { RequestType = "Request1" } }
            };

            var registry2 = new HandlerRegistryInfo
            {
                AssemblyName = "Assembly2",
                Handlers = { new HandlerInfo { RequestType = "Request2" } }
            };

            // Act & Assert
            Assert.Equal("Assembly1", registry1.AssemblyName);
            Assert.Equal("Assembly2", registry2.AssemblyName);
            Assert.Equal("Request1", registry1.Handlers[0].RequestType);
            Assert.Equal("Request2", registry2.Handlers[0].RequestType);
            Assert.Equal(1, registry1.TotalHandlers);
            Assert.Equal(1, registry2.TotalHandlers);
        }
    }
}
