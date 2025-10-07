using System;
using System.Threading.Tasks;
using FluentAssertions;
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
            unit.Should().Be(default(Unit));
        }

        [Fact]
        public async Task Unit_Task_ShouldReturnCompletedValueTask()
        {
            // Act
            var task = Unit.Task;
            var result = await task;

            // Assert
            task.IsCompleted.Should().BeTrue();
            result.Should().Be(Unit.Value);
        }

        [Fact]
        public void Unit_Equals_ShouldAlwaysReturnTrue()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = Unit.Value;

            // Act & Assert
            unit1.Equals(unit2).Should().BeTrue();
            unit1.Equals((object)unit2).Should().BeTrue();
        }

        [Fact]
        public void Unit_Equals_WithNonUnitObject_ShouldReturnFalse()
        {
            // Arrange
            var unit = Unit.Value;
            var other = new object();

            // Act & Assert
            unit.Equals(other).Should().BeFalse();
            unit.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void Unit_GetHashCode_ShouldReturnZero()
        {
            // Arrange
            var unit = Unit.Value;

            // Act
            var hashCode = unit.GetHashCode();

            // Assert
            hashCode.Should().Be(0);
        }

        [Fact]
        public void Unit_ToString_ShouldReturnEmptyTuple()
        {
            // Arrange
            var unit = Unit.Value;

            // Act
            var str = unit.ToString();

            // Assert
            str.Should().Be("()");
        }

        [Fact]
        public void Unit_EqualityOperator_ShouldAlwaysReturnTrue()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = Unit.Value;

            // Act & Assert
            (unit1 == unit2).Should().BeTrue();
        }

        [Fact]
        public void Unit_InequalityOperator_ShouldAlwaysReturnFalse()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = Unit.Value;

            // Act & Assert
            (unit1 != unit2).Should().BeFalse();
        }

        [Fact]
        public void Unit_ImplicitConversionFromValueTuple_ShouldWork()
        {
            // Act
            Unit unit = default(ValueTuple);

            // Assert
            unit.Should().Be(Unit.Value);
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
            task.IsCompleted.Should().BeTrue();
            result.Should().Be(Unit.Value);
        }

        [Fact]
        public async Task Unit_CanBeUsedAsReturnType()
        {
            // Act
            var result = await ReturnsUnitAsync();

            // Assert
            result.Should().Be(Unit.Value);
        }

        [Fact]
        public async Task Unit_Task_CanBeAwaitedDirectly()
        {
            // Act
            var result = await Unit.Task;

            // Assert
            result.Should().Be(Unit.Value);
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
                    unit1.Should().Be(unit2);
                    unit1.Equals(unit2).Should().BeTrue();
                    (unit1 == unit2).Should().BeTrue();
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
            defaultUnit.Should().Be(valueUnit);
        }

        private async ValueTask<Unit> ReturnsUnitAsync()
        {
            await Task.Delay(1);
            return Unit.Value;
        }
    }
}