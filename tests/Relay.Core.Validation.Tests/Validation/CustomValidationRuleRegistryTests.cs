using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation;

namespace Relay.Core.Tests.Validation
{
    public class CustomValidationRuleRegistryTests
    {
        [Fact]
        public void RegisterRule_Should_Throw_ArgumentException_When_Name_Is_Null()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                registry.RegisterRule(null!, (obj, ct) => new ValueTask<IEnumerable<string>>(Array.Empty<string>())));
        }

        [Fact]
        public void RegisterRule_Should_Throw_ArgumentException_When_Name_Is_Empty()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                registry.RegisterRule("", (obj, ct) => new ValueTask<IEnumerable<string>>(Array.Empty<string>())));
        }

        [Fact]
        public void RegisterRule_Should_Throw_ArgumentNullException_When_ValidationFunc_Is_Null()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                registry.RegisterRule("test", null!));
        }

        [Fact]
        public void RegisterRule_Should_Add_Rule_To_Registry()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();
            var validationFunc = (object obj, CancellationToken ct) => new ValueTask<IEnumerable<string>>(new[] { "error" });

            // Act
            registry.RegisterRule("testRule", validationFunc);

            // Assert
            Assert.True(registry.IsRuleRegistered("testRule"));
            var retrievedFunc = registry.GetRule("testRule");
            Assert.NotNull(retrievedFunc);
        }

        [Fact]
        public void GetRule_Should_Return_Null_When_Rule_Not_Found()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();

            // Act
            var result = registry.GetRule("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetRule_Should_Return_Registered_Function()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();
            var validationFunc = (object obj, CancellationToken ct) =>
            {
                var errors = new List<string>();
                if (obj is TestRequest request && request.Name.Length < 3)
                {
                    errors.Add("Name too short");
                }
                return new ValueTask<IEnumerable<string>>(errors);
            };

            registry.RegisterRule("nameLengthRule", validationFunc);

            // Act
            var retrievedFunc = registry.GetRule("nameLengthRule");
            var result = await retrievedFunc!(new TestRequest { Name = "AB" }, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal("Name too short", result.First());
        }

        [Fact]
        public void IsRuleRegistered_Should_Return_True_For_Registered_Rule()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();
            registry.RegisterRule("testRule", (obj, ct) => new ValueTask<IEnumerable<string>>(Array.Empty<string>()));

            // Act
            var result = registry.IsRuleRegistered("testRule");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRuleRegistered_Should_Return_False_For_Unregistered_Rule()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();

            // Act
            var result = registry.IsRuleRegistered("nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RemoveRule_Should_Return_True_When_Rule_Is_Removed()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();
            registry.RegisterRule("testRule", (obj, ct) => new ValueTask<IEnumerable<string>>(Array.Empty<string>()));

            // Act
            var result = registry.RemoveRule("testRule");

            // Assert
            Assert.True(result);
            Assert.False(registry.IsRuleRegistered("testRule"));
        }

        [Fact]
        public void RemoveRule_Should_Return_False_When_Rule_Not_Found()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();

            // Act
            var result = registry.RemoveRule("nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetRegisteredRuleNames_Should_Return_All_Registered_Names()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();
            registry.RegisterRule("rule1", (obj, ct) => new ValueTask<IEnumerable<string>>(Array.Empty<string>()));
            registry.RegisterRule("rule2", (obj, ct) => new ValueTask<IEnumerable<string>>(Array.Empty<string>()));

            // Act
            var names = registry.GetRegisteredRuleNames().ToList();

            // Assert
            Assert.Equal(2, names.Count);
            Assert.Contains("rule1", names);
            Assert.Contains("rule2", names);
        }

        [Fact]
        public void Clear_Should_Remove_All_Rules()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();
            registry.RegisterRule("rule1", (obj, ct) => new ValueTask<IEnumerable<string>>(Array.Empty<string>()));
            registry.RegisterRule("rule2", (obj, ct) => new ValueTask<IEnumerable<string>>(Array.Empty<string>()));

            // Act
            registry.Clear();

            // Assert
            Assert.Empty(registry.GetRegisteredRuleNames());
            Assert.False(registry.IsRuleRegistered("rule1"));
            Assert.False(registry.IsRuleRegistered("rule2"));
        }

        [Fact]
        public void RegisterRule_Should_Overwrite_Existing_Rule()
        {
            // Arrange
            var registry = new CustomValidationRuleRegistry();
            var firstFunc = (object obj, CancellationToken ct) => new ValueTask<IEnumerable<string>>(new[] { "first" });
            var secondFunc = (object obj, CancellationToken ct) => new ValueTask<IEnumerable<string>>(new[] { "second" });

            // Act
            registry.RegisterRule("testRule", firstFunc);
            registry.RegisterRule("testRule", secondFunc);

            var retrievedFunc = registry.GetRule("testRule");
            var result = retrievedFunc!(new object(), CancellationToken.None).Result;

            // Assert
            Assert.Single(result);
            Assert.Equal("second", result.First());
        }

        private class TestRequest
        {
            public string Name { get; set; } = "";
        }
    }
}