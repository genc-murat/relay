using Relay.Core.AI;
using Relay.Core.Contracts.Requests;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIMonitoredAttributeTests
    {
        [Fact]
        public void Constructor_CreatesInstanceWithDefaultValues()
        {
            // Act
            var attribute = new AIMonitoredAttribute();

            // Assert
            Assert.NotNull(attribute);
            Assert.IsType<AIMonitoredAttribute>(attribute);
        }

        [Fact]
        public void DefaultValues_AreSetCorrectly()
        {
            // Act
            var attribute = new AIMonitoredAttribute();

            // Assert
            Assert.Equal(MonitoringLevel.Standard, attribute.Level);
            Assert.True(attribute.CollectDetailedMetrics);
            Assert.True(attribute.TrackAccessPatterns);
            Assert.Equal(1.0, attribute.SamplingRate);
            Assert.Empty(attribute.Tags);
        }

        [Fact]
        public void Level_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIMonitoredAttribute();

            // Act
            attribute.Level = MonitoringLevel.Detailed;

            // Assert
            Assert.Equal(MonitoringLevel.Detailed, attribute.Level);
        }

        [Fact]
        public void CollectDetailedMetrics_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIMonitoredAttribute();

            // Act
            attribute.CollectDetailedMetrics = false;

            // Assert
            Assert.False(attribute.CollectDetailedMetrics);
        }

        [Fact]
        public void TrackAccessPatterns_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIMonitoredAttribute();

            // Act
            attribute.TrackAccessPatterns = false;

            // Assert
            Assert.False(attribute.TrackAccessPatterns);
        }

        [Fact]
        public void SamplingRate_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIMonitoredAttribute();

            // Act
            attribute.SamplingRate = 0.5;

            // Assert
            Assert.Equal(0.5, attribute.SamplingRate);
        }

        [Fact]
        public void Tags_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIMonitoredAttribute();
            var tags = new[] { "api", "critical" };

            // Act
            attribute.Tags = tags;

            // Assert
            Assert.Equal(tags, attribute.Tags);
        }

        [Fact]
        public void AttributeUsage_AllowsClassTarget()
        {
            // Arrange
            var attributeUsage = typeof(AIMonitoredAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.True(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
        }

        [Fact]
        public void AttributeUsage_AllowsInterfaceTarget()
        {
            // Arrange
            var attributeUsage = typeof(AIMonitoredAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.True(attributeUsage.ValidOn.HasFlag(AttributeTargets.Interface));
        }

        [Fact]
        public void AttributeUsage_DoesNotAllowMultiple()
        {
            // Arrange
            var attributeUsage = typeof(AIMonitoredAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.False(attributeUsage.AllowMultiple);
        }

        [Fact]
        public void AttributeUsage_InheritedIsDefault()
        {
            // Arrange
            var attributeUsage = typeof(AIMonitoredAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.True(attributeUsage.Inherited); // Default is true when not specified
        }

        [Fact]
        public void CanCreateAttributeWithAllPropertiesSet()
        {
            // Act
            var attribute = new AIMonitoredAttribute
            {
                Level = MonitoringLevel.Comprehensive,
                CollectDetailedMetrics = false,
                TrackAccessPatterns = false,
                SamplingRate = 0.8,
                Tags = new[] { "high-priority", "analytics" }
            };

            // Assert
            Assert.Equal(MonitoringLevel.Comprehensive, attribute.Level);
            Assert.False(attribute.CollectDetailedMetrics);
            Assert.False(attribute.TrackAccessPatterns);
            Assert.Equal(0.8, attribute.SamplingRate);
            Assert.Equal(2, attribute.Tags.Length);
            Assert.Contains("high-priority", attribute.Tags);
            Assert.Contains("analytics", attribute.Tags);
        }

        [Fact]
        public void CanCreateAttributeWithMinimalConfiguration()
        {
            // Act
            var attribute = new AIMonitoredAttribute
            {
                Level = MonitoringLevel.Basic
            };

            // Assert
            Assert.Equal(MonitoringLevel.Basic, attribute.Level);
            // Other properties should retain defaults
            Assert.True(attribute.CollectDetailedMetrics);
            Assert.True(attribute.TrackAccessPatterns);
            Assert.Equal(1.0, attribute.SamplingRate);
            Assert.Empty(attribute.Tags);
        }

        [Fact]
        public void Tags_DefaultsToEmptyArray()
        {
            // Act
            var attribute = new AIMonitoredAttribute();

            // Assert
            Assert.NotNull(attribute.Tags);
            Assert.Empty(attribute.Tags);
        }

        [Fact]
        public void CanSetTagsToNull_ResultsInEmptyArray()
        {
            // Arrange
            var attribute = new AIMonitoredAttribute();

            // Act
            attribute.Tags = null!;

            // Assert
            Assert.Null(attribute.Tags);
        }

        // Test classes and interfaces with attributes for usage testing
        [AIMonitored]
        private class TestClassWithAttribute
        {
        }

        [AIMonitored(
            Level = MonitoringLevel.Detailed,
            CollectDetailedMetrics = false,
            SamplingRate = 0.9,
            Tags = new[] { "test", "sample" })]
        private class TestClassWithCustomAttribute
        {
        }

        private class TestClassWithoutAttribute
        {
        }

        [AIMonitored]
        private interface ITestInterfaceWithAttribute
        {
        }

        private interface ITestInterfaceWithoutAttribute
        {
        }

        [Fact]
        public void CanApplyAttributeToClass()
        {
            // Act
            var attribute = typeof(TestClassWithAttribute)
                .GetCustomAttribute<AIMonitoredAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void CanApplyAttributeToInterface()
        {
            // Act
            var attribute = typeof(ITestInterfaceWithAttribute)
                .GetCustomAttribute<AIMonitoredAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void ClassWithoutAttribute_DoesNotHaveAttribute()
        {
            // Act
            var attribute = typeof(TestClassWithoutAttribute)
                .GetCustomAttribute<AIMonitoredAttribute>();

            // Assert
            Assert.Null(attribute);
        }

        [Fact]
        public void InterfaceWithoutAttribute_DoesNotHaveAttribute()
        {
            // Act
            var attribute = typeof(ITestInterfaceWithoutAttribute)
                .GetCustomAttribute<AIMonitoredAttribute>();

            // Assert
            Assert.Null(attribute);
        }

        [Fact]
        public void AttributeOnClass_HasDefaultValues()
        {
            // Act
            var attribute = typeof(TestClassWithAttribute)
                .GetCustomAttribute<AIMonitoredAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal(MonitoringLevel.Standard, attribute.Level);
            Assert.True(attribute.CollectDetailedMetrics);
            Assert.True(attribute.TrackAccessPatterns);
            Assert.Equal(1.0, attribute.SamplingRate);
            Assert.Empty(attribute.Tags);
        }

        [Fact]
        public void AttributeWithCustomConfiguration_HasCorrectValues()
        {
            // Act
            var attribute = typeof(TestClassWithCustomAttribute)
                .GetCustomAttribute<AIMonitoredAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal(MonitoringLevel.Detailed, attribute.Level);
            Assert.False(attribute.CollectDetailedMetrics);
            Assert.True(attribute.TrackAccessPatterns); // Default value
            Assert.Equal(0.9, attribute.SamplingRate);
            Assert.Equal(2, attribute.Tags.Length);
            Assert.Contains("test", attribute.Tags);
            Assert.Contains("sample", attribute.Tags);
        }

        [Fact]
        public void AttributeType_IsSealed()
        {
            // Act
            var isSealed = typeof(AIMonitoredAttribute).IsSealed;

            // Assert
            Assert.True(isSealed);
        }

        [Fact]
        public void Attribute_InheritsFromAttribute()
        {
            // Act
            var baseType = typeof(AIMonitoredAttribute).BaseType;

            // Assert
            Assert.Equal(typeof(Attribute), baseType);
        }

        [Fact]
        public void SamplingRate_ValidRange()
        {
            // Arrange
            var attribute = new AIMonitoredAttribute();

            // Act & Assert - Valid values
            attribute.SamplingRate = 0.0;
            Assert.Equal(0.0, attribute.SamplingRate);

            attribute.SamplingRate = 1.0;
            Assert.Equal(1.0, attribute.SamplingRate);

            attribute.SamplingRate = 0.5;
            Assert.Equal(0.5, attribute.SamplingRate);
        }

        [Fact]
        public void MonitoringLevel_AllEnumValuesSupported()
        {
            // Test all enum values can be set
            var attribute = new AIMonitoredAttribute();

            foreach (MonitoringLevel level in Enum.GetValues(typeof(MonitoringLevel)))
            {
                attribute.Level = level;
                Assert.Equal(level, attribute.Level);
            }
        }

        // Integration tests with actual usage scenarios
        [AIMonitored(Level = MonitoringLevel.Basic, CollectDetailedMetrics = false, SamplingRate = 0.5, Tags = new[] { "test-integration" })]
        private class IntegrationTestRequest : IRequest<string>
        {
        }

        private class NonMonitoredRequest : IRequest<string>
        {
        }

        [Fact]
        public void IntegrationTest_AttributeAppliedToRealRequestType()
        {
            // Act
            var attribute = typeof(IntegrationTestRequest).GetCustomAttribute<AIMonitoredAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal(MonitoringLevel.Basic, attribute.Level);
            Assert.False(attribute.CollectDetailedMetrics);
            Assert.Equal(0.5, attribute.SamplingRate);
            Assert.Contains("test-integration", attribute.Tags);
        }

        [Fact]
        public void IntegrationTest_NonMonitoredRequest_HasNoAttribute()
        {
            // Act
            var attribute = typeof(NonMonitoredRequest).GetCustomAttribute<AIMonitoredAttribute>();

            // Assert
            Assert.Null(attribute);
        }

        [Fact]
        public void IntegrationTest_AttributeInheritance_WorksForInterfaces()
        {
            // Test that interfaces can also be monitored
            var attribute = typeof(ITestInterfaceWithAttribute).GetCustomAttribute<AIMonitoredAttribute>();
            Assert.NotNull(attribute);
        }
    }
}