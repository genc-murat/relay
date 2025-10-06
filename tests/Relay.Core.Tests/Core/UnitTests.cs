using System;
using System.Threading.Tasks;
using Xunit;
using Relay.Core;
using Relay.Core.Contracts.Infrastructure;

namespace Relay.Core.Tests
{
    public class UnitTests
    {
        [Fact]
        public void Value_ShouldReturnDefaultUnit()
        {
            // Act
            var unit = Unit.Value;

            // Assert
            Assert.Equal(default(Unit), unit);
        }

        [Fact]
        public async Task Task_ShouldReturnCompletedValueTask()
        {
            // Act
            var result = await Unit.Task;

            // Assert
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public void Equals_WithSameUnit_ShouldReturnTrue()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = Unit.Value;

            // Act & Assert
            Assert.True(unit1.Equals(unit2));
        }

        [Fact]
        public void Equals_WithObject_WhenObjectIsUnit_ShouldReturnTrue()
        {
            // Arrange
            var unit = Unit.Value;
            object obj = Unit.Value;

            // Act & Assert
            Assert.True(unit.Equals(obj));
        }

        [Fact]
        public void Equals_WithObject_WhenObjectIsNotUnit_ShouldReturnFalse()
        {
            // Arrange
            var unit = Unit.Value;
            object obj = new object();

            // Act & Assert
            Assert.False(unit.Equals(obj));
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var unit = Unit.Value;

            // Act & Assert
            Assert.False(unit.Equals(null));
        }

        [Fact]
        public void GetHashCode_ShouldReturnZero()
        {
            // Arrange
            var unit = Unit.Value;

            // Act
            var hashCode = unit.GetHashCode();

            // Assert
            Assert.Equal(0, hashCode);
        }

        [Fact]
        public void ToString_ShouldReturnEmptyParentheses()
        {
            // Arrange
            var unit = Unit.Value;

            // Act
            var result = unit.ToString();

            // Assert
            Assert.Equal("()", result);
        }

        [Fact]
        public void EqualityOperator_ShouldReturnTrue()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = Unit.Value;

            // Act & Assert
            Assert.True(unit1 == unit2);
        }

        [Fact]
        public void InequalityOperator_ShouldReturnFalse()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = Unit.Value;

            // Act & Assert
            Assert.False(unit1 != unit2);
        }

        [Fact]
        public void ImplicitConversion_FromValueTuple_ShouldReturnUnit()
        {
            // Act
            Unit unit = ValueTuple.Create();

            // Assert
            Assert.Equal(Unit.Value, unit);
        }

        [Fact]
        public async Task ImplicitConversion_ToValueTask_ShouldReturnCompletedTask()
        {
            // Arrange
            var unit = Unit.Value;

            // Act
            ValueTask<Unit> task = unit;
            var result = await task;

            // Assert
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public void MultipleInstances_ShouldBeEqual()
        {
            // Arrange
            var unit1 = default(Unit);
            var unit2 = new Unit();
            var unit3 = Unit.Value;

            // Assert
            Assert.Equal(unit1, unit2);
            Assert.Equal(unit2, unit3);
            Assert.Equal(unit1, unit3);
        }
    }
}
