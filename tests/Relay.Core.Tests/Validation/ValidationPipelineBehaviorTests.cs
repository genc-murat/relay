using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Relay.Core.Validation;

namespace Relay.Core.Tests.Validation
{
    public class ValidationPipelineBehaviorTests
    {
        [Fact]
        public async Task Should_Pass_Validation_When_Request_Is_Valid()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();
            
            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);
            
            var request = "valid request";
            var next = new RequestHandlerDelegate<int>(() => new ValueTask<int>(42));
            
            // Act
            var result = await behavior.HandleAsync(request, next, default);
            
            // Assert
            Assert.Equal(42, result);
        }
        
        [Fact]
        public async Task Should_Throw_ValidationException_When_Request_Is_Invalid()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();
            
            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);
            
            var request = "";
            var next = new RequestHandlerDelegate<int>(() => new ValueTask<int>(42));
            
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () => 
                await behavior.HandleAsync(request, next, default));
        }
    }
}