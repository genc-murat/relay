using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using System;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class DIIntegrationTests
    {
        [Fact]
        public void ServiceCollection_CanRegisterRelayServices()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act & Assert - This should compile without errors
            // In a real scenario, this would use the generated AddRelay extension method
            services.AddSingleton<IRelay, RelayImplementation>();
            
            var serviceProvider = services.BuildServiceProvider();
            var relay = serviceProvider.GetService<IRelay>();
            
            Assert.NotNull(relay);
            Assert.IsType<RelayImplementation>(relay);
        }

        [Fact]
        public void RelayImplementation_CanBeConstructed()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var relay = new RelayImplementation(serviceProvider);
            
            // Assert
            Assert.NotNull(relay);
        }

        [Fact]
        public void RelayImplementation_ThrowsForNullServiceProvider()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RelayImplementation(null!));
        }
    }
}