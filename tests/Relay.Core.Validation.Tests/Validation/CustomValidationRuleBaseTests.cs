using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class CustomValidationRuleBaseTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Errors_From_Derived_Class()
        {
            // Arrange
            var rule = new TestCustomValidationRule();

            var request = new TestRequest { Name = "", Age = 16 };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Equal(2, errors.Count());
            Assert.Contains("Name is required", errors);
            Assert.Contains("Must be at least 18 years old", errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Validation_Passes()
        {
            // Arrange
            var rule = new TestCustomValidationRule();

            var request = new TestRequest { Name = "Valid Name", Age = 25 };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_CancellationToken()
        {
            // Arrange
            var rule = new TestCustomValidationRule();

            var request = new TestRequest { Name = "Valid", Age = 25 };
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync(request, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Respect_CancellationToken()
        {
            // Arrange
            var rule = new AsyncTestCustomValidationRule();

            var request = new TestRequest { Name = "Valid", Age = 25 };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await rule.ValidateAsync(request, cts.Token));
        }

        [Fact]
        public async Task AddErrorIf_With_Bool_Condition_Should_Add_Error_When_True()
        {
            // Arrange
            var rule = new SimpleTestCustomValidationRule();

            var request = new TestRequest { Name = "", Age = 25 };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name is required", errors.First());
        }

        [Fact]
        public async Task AddErrorIf_With_Func_Condition_Should_Add_Error_When_True()
        {
            // Arrange
            var rule = new FuncTestCustomValidationRule();

            var request = new TestRequest { Name = "Valid", Age = 16 };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Must be at least 18 years old", errors.First());
        }

        private class TestCustomValidationRule : CustomValidationRuleBase<TestRequest>
        {
            protected override async ValueTask ValidateCoreAsync(TestRequest request, List<string> errors, CancellationToken cancellationToken)
            {
                await Task.Delay(1, cancellationToken); // Simulate async work

                AddErrorIf(string.IsNullOrEmpty(request.Name), "Name is required", errors);
                AddErrorIf(request.Age < 18, "Must be at least 18 years old", errors);
            }
        }

        private class AsyncTestCustomValidationRule : CustomValidationRuleBase<TestRequest>
        {
            protected override async ValueTask ValidateCoreAsync(TestRequest request, List<string> errors, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken);
                // No errors for this test
            }
        }

        private class SimpleTestCustomValidationRule : CustomValidationRuleBase<TestRequest>
        {
            protected override ValueTask ValidateCoreAsync(TestRequest request, List<string> errors, CancellationToken cancellationToken)
            {
                AddErrorIf(string.IsNullOrEmpty(request.Name), "Name is required", errors);
                return ValueTask.CompletedTask;
            }
        }

        private class FuncTestCustomValidationRule : CustomValidationRuleBase<TestRequest>
        {
            protected override ValueTask ValidateCoreAsync(TestRequest request, List<string> errors, CancellationToken cancellationToken)
            {
                AddErrorIf(() => request.Age < 18, "Must be at least 18 years old", errors);
                return ValueTask.CompletedTask;
            }
        }

        private class TestRequest
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
        }
    }
}