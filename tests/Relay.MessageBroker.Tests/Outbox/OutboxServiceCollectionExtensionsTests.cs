using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.MessageBroker.Outbox;
using Scrutor;

namespace Relay.MessageBroker.Tests.Outbox;

public class OutboxServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOutboxPattern_WithDefaultConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>(); // OutboxWorker depends on IMessageBroker

        // Act
        services.AddOutboxPattern();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var outboxStore = serviceProvider.GetService<IOutboxStore>();
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OutboxOptions>>().Value;

        Assert.NotNull(outboxStore);
        Assert.NotNull(options);
        Assert.IsType<InMemoryOutboxStore>(outboxStore);
        Assert.Contains(hostedServices, s => s is OutboxWorker);
        Assert.True(options.Enabled); // Default when no config provided
    }

    [Fact]
    public void AddOutboxPattern_WithCustomConfiguration_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddOutboxPattern(options =>
        {
            options.Enabled = true;
            options.BatchSize = 100;
            options.PollingInterval = TimeSpan.FromSeconds(5);
            options.MaxRetryAttempts = 3;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OutboxOptions>>().Value;

        Assert.True(options.Enabled);
        Assert.Equal(100, options.BatchSize);
        Assert.Equal(TimeSpan.FromSeconds(5), options.PollingInterval);
        Assert.Equal(3, options.MaxRetryAttempts);
    }

    [Fact]
    public void AddOutboxPattern_WithNullConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddOutboxPattern(null);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OutboxOptions>>().Value;

        Assert.True(options.Enabled); // Default when no config provided
    }

    [Fact]
    public void AddOutboxPatternWithSql_WithValidConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>(); // OutboxWorker depends on IMessageBroker

        // Act
        services.AddOutboxPatternWithSql(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var outboxStore = serviceProvider.GetService<IOutboxStore>();
        var dbContextFactory = serviceProvider.GetService<IDbContextFactory<OutboxDbContext>>();
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OutboxOptions>>().Value;

        Assert.NotNull(outboxStore);
        Assert.NotNull(dbContextFactory);
        Assert.NotNull(options);
        Assert.IsType<SqlOutboxStore>(outboxStore);
        Assert.Contains(hostedServices, s => s is OutboxWorker);
        Assert.True(options.Enabled); // Default when no config provided
    }

    [Fact]
    public void AddOutboxPatternWithSql_WithNullConfigureDbContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddOutboxPatternWithSql(null!));
    }

    [Fact]
    public void AddOutboxPatternWithSql_WithCustomConfiguration_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddOutboxPatternWithSql(
            options => options.UseInMemoryDatabase("TestDb"),
            outboxOptions =>
            {
                outboxOptions.Enabled = false;
                outboxOptions.BatchSize = 200;
                outboxOptions.PollingInterval = TimeSpan.FromSeconds(10);
                outboxOptions.MaxRetryAttempts = 5;
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OutboxOptions>>().Value;

        Assert.False(options.Enabled);
        Assert.Equal(200, options.BatchSize);
        Assert.Equal(TimeSpan.FromSeconds(10), options.PollingInterval);
        Assert.Equal(5, options.MaxRetryAttempts);
    }

    [Fact]
    public void DecorateMessageBrokerWithOutbox_ShouldDecorateMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddOutboxPattern(); // Need outbox store

        // Act
        services.DecorateMessageBrokerWithOutbox();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var messageBroker = serviceProvider.GetService<IMessageBroker>();

        Assert.NotNull(messageBroker);
        Assert.IsType<OutboxMessageBrokerDecorator>(messageBroker);
    }

    [Fact]
    public void DecorateMessageBrokerWithOutbox_WithoutMessageBroker_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should throw because there's no message broker to decorate
        Assert.Throws<DecorationException>(() =>
            services.DecorateMessageBrokerWithOutbox());
    }
}