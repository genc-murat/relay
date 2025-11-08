using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Testing.Tests.Core;

public class RelayCollectionFixtureTests
{
    private class TestRelayCollectionFixture : RelayCollectionFixture
    {
        public bool ConfigureSharedTestRelayCalled { get; private set; }
        public bool OnSharedTestInitializedAsyncCalled { get; private set; }
        public bool OnSharedTestCleanupAsyncCalled { get; private set; }

        protected override void ConfigureSharedTestRelay(TestRelay testRelay)
        {
            ConfigureSharedTestRelayCalled = true;
            base.ConfigureSharedTestRelay(testRelay);
        }

        protected override Task OnSharedTestInitializedAsync()
        {
            OnSharedTestInitializedAsyncCalled = true;
            return base.OnSharedTestInitializedAsync();
        }

        protected override Task OnSharedTestCleanupAsync()
        {
            OnSharedTestCleanupAsyncCalled = true;
            return base.OnSharedTestCleanupAsync();
        }
    }

    [Fact]
    public void TestRelay_WhenAccessedBeforeInitialization_ThrowsInvalidOperationException()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _ = fixture.TestRelay);
        Assert.Contains("Shared TestRelay not initialized", exception.Message);
    }

    [Fact]
    public void Services_WhenAccessedBeforeInitialization_ThrowsInvalidOperationException()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _ = fixture.Services);
        Assert.Contains("Shared services not initialized", exception.Message);
    }

    [Fact]
    public async Task InitializeAsync_CreatesTestRelayAndServices()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();

        // Act
        await fixture.InitializeAsync();

        // Assert
        Assert.NotNull(fixture.TestRelay);
        Assert.NotNull(fixture.Services);
        Assert.IsType<TestRelay>(fixture.TestRelay);
        Assert.IsAssignableFrom<IServiceProvider>(fixture.Services);
    }

    [Fact]
    public async Task InitializeAsync_CallsVirtualMethods()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();

        // Act
        await fixture.InitializeAsync();

        // Assert
        Assert.True(fixture.ConfigureSharedTestRelayCalled);
        Assert.True(fixture.OnSharedTestInitializedAsyncCalled);
    }

    [Fact]
    public async Task DisposeAsync_CleansUpResources()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();
        await fixture.InitializeAsync();

        // Act
        await fixture.DisposeAsync();

        // Assert - Properties should throw after dispose
        var testRelayException = Assert.Throws<InvalidOperationException>(() => _ = fixture.TestRelay);
        Assert.Contains("Shared TestRelay not initialized", testRelayException.Message);

        var servicesException = Assert.Throws<InvalidOperationException>(() => _ = fixture.Services);
        Assert.Contains("Shared services not initialized", servicesException.Message);
    }

    [Fact]
    public async Task DisposeAsync_CallsVirtualCleanupMethod()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();
        await fixture.InitializeAsync();

        // Act
        await fixture.DisposeAsync();

        // Assert
        Assert.True(fixture.OnSharedTestCleanupAsyncCalled);
    }

    [Fact]
    public async Task DisposeAsync_ClearsTestRelay()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();
        await fixture.InitializeAsync();
        var testRelay = fixture.TestRelay;

        // Act
        await fixture.DisposeAsync();

        // Assert - TestRelay should be cleared (implementation detail, but we can verify it's disposed)
        // Since TestRelay.Clear() is called, we can't easily verify internal state,
        // but we can verify the property throws after dispose
        var exception = Assert.Throws<InvalidOperationException>(() => _ = fixture.TestRelay);
        Assert.Contains("Shared TestRelay not initialized", exception.Message);
    }

    [Fact]
    public async Task MultipleInitializeCalls_ThrowAfterFirstDispose()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();
        await fixture.InitializeAsync();
        await fixture.DisposeAsync();

        // Act & Assert - Accessing after dispose should throw
        var exception = Assert.Throws<InvalidOperationException>(() => _ = fixture.TestRelay);
        Assert.Contains("Shared TestRelay not initialized", exception.Message);

        exception = Assert.Throws<InvalidOperationException>(() => _ = fixture.Services);
        Assert.Contains("Shared services not initialized", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();
        await fixture.InitializeAsync();

        // Act - Dispose multiple times
        await fixture.DisposeAsync();
        await fixture.DisposeAsync(); // Should not throw

        // Assert - Properties should still throw
        var testRelayException = Assert.Throws<InvalidOperationException>(() => _ = fixture.TestRelay);
        Assert.Contains("Shared TestRelay not initialized", testRelayException.Message);
    }

    [Fact]
    public async Task DisposeAsync_WithoutInitialize_DoesNotThrow()
    {
        // Arrange
        var fixture = new TestRelayCollectionFixture();

        // Act - Dispose without initialize
        await fixture.DisposeAsync(); // Should not throw

        // Assert - Properties should still throw (never initialized)
        var testRelayException = Assert.Throws<InvalidOperationException>(() => _ = fixture.TestRelay);
        Assert.Contains("Shared TestRelay not initialized", testRelayException.Message);
    }

    [Fact]
    public void RelayTestCollection_IsDefined()
    {
        // Arrange & Act
        var collectionType = typeof(RelayTestCollection);

        // Assert
        Assert.NotNull(collectionType);
        var collectionDefinition = collectionType.GetCustomAttributes(typeof(CollectionDefinitionAttribute), false);
        Assert.Single(collectionDefinition);
    }
}