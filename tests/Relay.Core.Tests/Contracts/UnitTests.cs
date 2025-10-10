using System;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Infrastructure;
using Xunit;

namespace Relay.Core.Tests.Contracts
{
    public class UnitTests
    {
        [Fact]
        public void Unit_Value_ShouldReturnDefaultInstance()
        {
            // Act
            var unit = Unit.Value;

            // Assert
            Assert.Equal(default(Unit), unit);
        }

        [Fact]
        public async Task Unit_Task_ShouldReturnCompletedValueTask()
        {
            // Act
            var task = Unit.Task;
            var result = await task;

            // Assert
            Assert.True(task.IsCompleted);
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public void Unit_Equals_ShouldAlwaysReturnTrue()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = Unit.Value;

            // Act & Assert
            Assert.True(unit1.Equals(unit2));
            Assert.True(unit1.Equals((object)unit2));
        }

        [Fact]
        public void Unit_Equals_WithNonUnitObject_ShouldReturnFalse()
        {
            // Arrange
            var unit = Unit.Value;
            var other = new object();

            // Act & Assert
            Assert.False(unit.Equals(other));
            Assert.False(unit.Equals(null));
        }

        [Fact]
        public void Unit_GetHashCode_ShouldReturnZero()
        {
            // Arrange
            var unit = Unit.Value;

            // Act
            var hashCode = unit.GetHashCode();

            // Assert
            Assert.Equal(0, hashCode);
        }

        [Fact]
        public void Unit_ToString_ShouldReturnEmptyTuple()
        {
            // Arrange
            var unit = Unit.Value;

            // Act
            var str = unit.ToString();

            // Assert
            Assert.Equal("()", str);
        }

        [Fact]
        public void Unit_EqualityOperator_ShouldAlwaysReturnTrue()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = Unit.Value;

            // Act & Assert
            Assert.True(unit1 == unit2);
        }

        [Fact]
        public void Unit_InequalityOperator_ShouldAlwaysReturnFalse()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = Unit.Value;

            // Act & Assert
            Assert.False(unit1 != unit2);
        }

        [Fact]
        public void Unit_ImplicitConversionFromValueTuple_ShouldWork()
        {
            // Act
            Unit unit = default(ValueTuple);

            // Assert
            Assert.Equal(Unit.Value, unit);
        }

        [Fact]
        public async Task Unit_ImplicitConversionToValueTask_ShouldWork()
        {
            // Arrange
            var unit = Unit.Value;

            // Act
            ValueTask<Unit> task = unit;
            var result = await task;

            // Assert
            Assert.True(task.IsCompleted);
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Unit_CanBeUsedAsReturnType()
        {
            // Act
            var result = await ReturnsUnitAsync();

            // Assert
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Unit_Task_CanBeAwaitedDirectly()
        {
            // Act
            var result = await Unit.Task;

            // Assert
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public void Unit_MultipleInstances_ShouldBeEqual()
        {
            // Arrange
            var units = new[] { Unit.Value, default(Unit), new Unit() };

            // Act & Assert
            foreach (var unit1 in units)
            {
                foreach (var unit2 in units)
                {
                    Assert.Equal(unit1, unit2);
                    Assert.True(unit1.Equals(unit2));
                    Assert.True(unit1 == unit2);
                }
            }
        }

        [Fact]
        public void Unit_Default_ShouldEqualValue()
        {
            // Act
            var defaultUnit = default(Unit);
            var valueUnit = Unit.Value;

            // Assert
            Assert.Equal(valueUnit, defaultUnit);
        }

        private async ValueTask<Unit> ReturnsUnitAsync()
        {
            await Task.Delay(1);
            return Unit.Value;
        }
    }
}