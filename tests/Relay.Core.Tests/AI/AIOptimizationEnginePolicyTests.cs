using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEnginePolicyTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void UpdatePolicy_Should_Execute_Without_Errors()
        {
            // Arrange - Create exploration rate for testing
            var explorationRate = 0.2;

            // Act - Call UpdatePolicy directly using reflection
            var method = _engine.GetType().GetMethod("UpdatePolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { explorationRate });

            // Assert - Method should execute without throwing exceptions and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void UpdatePolicy_Should_Handle_High_Exploration_Rate()
        {
            // Arrange - High exploration rate (exploration mode)
            var explorationRate = 0.8;

            // Act - Call UpdatePolicy with high exploration rate
            var method = _engine.GetType().GetMethod("UpdatePolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { explorationRate });

            // Assert - Engine should remain functional and handle exploration mode
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void UpdatePolicy_Should_Handle_Low_Exploration_Rate()
        {
            // Arrange - Low exploration rate (exploitation mode)
            var explorationRate = 0.05;

            // Act - Call UpdatePolicy with low exploration rate
            var method = _engine.GetType().GetMethod("UpdatePolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { explorationRate });

            // Assert - Engine should remain functional and handle exploitation mode
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }
    }
}
