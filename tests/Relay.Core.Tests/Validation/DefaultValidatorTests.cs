using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Relay.Core.Validation;
using Relay.Core.Validation.Attributes;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Tests.Validation
{
    public class DefaultValidatorTests
    {
        [Fact]
        public async Task Constructor_Should_Throw_ArgumentNullException_When_ValidationRules_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DefaultValidator<string>(null!));
        }

        [Fact]
        public async Task ValidateAsync_Should_Throw_ArgumentNullException_When_Request_Is_Null()
        {
            // Arrange
            var validator = new DefaultValidator<string>(Array.Empty<IValidationRule<string>>());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await validator.ValidateAsync(null!));
            Assert.Equal("request", exception.ParamName);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_No_Validation_Rules()
        {
            // Arrange
            var validator = new DefaultValidator<string>(Array.Empty<IValidationRule<string>>());
            var request = "test";

            // Act
            var errors = await validator.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Execute_All_Validation_Rules()
        {
            // Arrange
            var rule1 = new Mock<IValidationRule<string>>();
            rule1.Setup(r => r.ValidateAsync("test", default)).ReturnsAsync(new[] { "Error 1" });

            var rule2 = new Mock<IValidationRule<string>>();
            rule2.Setup(r => r.ValidateAsync("test", default)).ReturnsAsync(new[] { "Error 2" });

            var validator = new DefaultValidator<string>(new IValidationRule<string>[] { rule1.Object, rule2.Object });

            // Act
            var errors = await validator.ValidateAsync("test");

            // Assert
            Assert.Equal(2, errors.Count());
            Assert.Contains("Error 1", errors);
            Assert.Contains("Error 2", errors);
            rule1.Verify(r => r.ValidateAsync("test", default), Times.Once);
            rule2.Verify(r => r.ValidateAsync("test", default), Times.Once);
        }

        [Fact]
        public async Task ValidateAsync_Should_Order_Rules_With_Default_Order_Zero()
        {
            // Arrange
            var executionOrder = new List<string>();

            var rule1 = new TestRule("RuleWithoutAttribute", executionOrder);
            var rule2 = new TestRuleWithOrder1("RuleWithOrder1", executionOrder);

            var validator = new DefaultValidator<string>(new IValidationRule<string>[] { rule1, rule2 });

            // Act
            await validator.ValidateAsync("test");

            // Assert - RuleWithoutAttribute (Order=0) should come before RuleWithOrder1 (Order=1)
            Assert.Equal(new[] { "RuleWithoutAttribute", "RuleWithOrder1" }, executionOrder);
        }

        [Fact]
        public async Task ValidateAsync_Should_Stop_On_Error_When_Rule_Has_ContinueOnError_False()
        {
            // Arrange
            var rule1 = new RuleWithContinueOnErrorFalse();
            var rule2 = new RuleWithContinueOnErrorTrue();

            var validator = new DefaultValidator<string>(new IValidationRule<string>[] { rule1, rule2 });

            // Act
            var errors = await validator.ValidateAsync("test");

            // Assert
            Assert.Single(errors);
            Assert.Contains("Error from RuleWithContinueOnErrorFalse", errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Continue_On_Error_When_Rule_Has_ContinueOnError_True()
        {
            // Arrange
            var rule1 = new RuleWithContinueOnErrorTrue();
            var rule2 = new RuleWithContinueOnErrorTrue();

            var validator = new DefaultValidator<string>(new IValidationRule<string>[] { rule1, rule2 });

            // Act
            var errors = await validator.ValidateAsync("test");

            // Assert
            Assert.Equal(2, errors.Count());
            Assert.Contains("Error from RuleWithContinueOnErrorTrue", errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Exception_In_Rule_And_Stop_Validation()
        {
            // Arrange
            var rule1 = new ExceptionRuleWithStop();
            var rule2 = new RuleWithContinueOnErrorTrue();

            var validator = new DefaultValidator<string>(new IValidationRule<string>[] { rule1, rule2 });

            // Act
            var errors = await validator.ValidateAsync("test");

            // Assert
            Assert.Single(errors);
            Assert.Contains("Validation rule 'ExceptionRuleWithStop' threw an exception: Rule failed", errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Exception_In_Rule_And_Continue_When_ContinueOnError_True()
        {
            // Arrange
            var rule1 = new ExceptionRuleWithContinue();
            var rule2 = new RuleWithContinueOnErrorTrue();

            var validator = new DefaultValidator<string>(new IValidationRule<string>[] { rule1, rule2 });

            // Act
            var errors = await validator.ValidateAsync("test");

            // Assert
            Assert.Equal(2, errors.Count());
            Assert.Contains("Validation rule 'ExceptionRuleWithContinue' threw an exception: Rule failed", errors);
            Assert.Contains("Error from RuleWithContinueOnErrorTrue", errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken_To_Rules()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var rule = new Mock<IValidationRule<string>>();
            rule.Setup(r => r.ValidateAsync("test", cts.Token)).ReturnsAsync(Array.Empty<string>());

            var validator = new DefaultValidator<string>(new IValidationRule<string>[] { rule.Object });

            // Act
            await validator.ValidateAsync("test", cts.Token);

            // Assert
            rule.Verify(r => r.ValidateAsync("test", cts.Token), Times.Once);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Rules_Without_ValidationRuleAttribute()
        {
            // Arrange
            var rule = new RuleWithoutAttribute();

            var validator = new DefaultValidator<string>(new IValidationRule<string>[] { rule });

            // Act
            var errors = await validator.ValidateAsync("test");

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Order_Rules_By_ValidationRuleAttribute_Order()
        {
            // Arrange
            var executionOrder = new List<string>();
            var rule1 = new TestRule("RuleWithoutAttribute", executionOrder);
            var rule2 = new TestRuleWithOrder1("RuleWithOrder1", executionOrder);

            var validator = new DefaultValidator<string>(new IValidationRule<string>[] { rule1, rule2 });

            // Act
            await validator.ValidateAsync("test");

            // Assert - RuleWithoutAttribute (Order=0) should come before RuleWithOrder1 (Order=1)
            Assert.Equal(new[] { "RuleWithoutAttribute", "RuleWithOrder1" }, executionOrder);
        }

        [ValidationRule(Order = 1)]
        private class RuleWithOrder1 : IValidationRule<string>
        {
            public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }
        }

        [ValidationRule(ContinueOnError = false)]
        private class RuleWithContinueOnErrorFalse : IValidationRule<string>
        {
            public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Error from RuleWithContinueOnErrorFalse" });
            }
        }

        [ValidationRule(ContinueOnError = true)]
        private class RuleWithContinueOnErrorTrue : IValidationRule<string>
        {
            public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Error from RuleWithContinueOnErrorTrue" });
            }
        }

        private class RuleWithoutAttribute : IValidationRule<string>
        {
            public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }
        }

        [ValidationRule(Order = 1)]
        private class TestRuleWithOrder1 : IValidationRule<string>
        {
            private readonly string _name;
            private readonly List<string> _executionOrder;

            public TestRuleWithOrder1(string name = "TestRuleWithOrder1", List<string> executionOrder = null)
            {
                _name = name;
                _executionOrder = executionOrder ?? new List<string>();
            }

            public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
            {
                _executionOrder.Add(_name);
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }
        }

        private class TestRule : IValidationRule<string>
        {
            private readonly string _name;
            private readonly List<string> _executionOrder;
            private readonly int _order;

            public TestRule(string name = "TestRule", List<string> executionOrder = null, int order = 0)
            {
                _name = name;
                _executionOrder = executionOrder ?? new List<string>();
                _order = order;
            }

            public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
            {
                _executionOrder.Add(_name);
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }
        }

        [ValidationRule(ContinueOnError = true)]
        private class ExceptionRuleWithContinue : IValidationRule<string>
        {
            public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Rule failed");
            }
        }

        [ValidationRule(ContinueOnError = false)]
        private class ExceptionRuleWithStop : IValidationRule<string>
        {
            public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Rule failed");
            }
        }


    }
}