using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Relay.MessageBroker.Inbox;
using Scrutor;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class InboxServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInboxPattern_WithDefaultConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInboxPattern();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var inboxStore = serviceProvider.GetService<IInboxStore>();
        var hostedServices = serviceProvider.GetServices<IHostedService>();

        Assert.NotNull(inboxStore);
        Assert.IsType<InMemoryInboxStore>(inboxStore);
        Assert.Contains(hostedServices, s => s is InboxCleanupWorker);
    }

    [Fact]
    public void AddInboxPattern_WithCustomConfiguration_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInboxPattern(options =>
        {
            options.Enabled = true;
            options.RetentionPeriod = TimeSpan.FromDays(14);
            options.ConsumerName = "CustomConsumer";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<InboxOptions>>().Value;

        Assert.True(options.Enabled);
        Assert.Equal(TimeSpan.FromDays(14), options.RetentionPeriod);
        Assert.Equal("CustomConsumer", options.ConsumerName);
    }

    [Fact]
    public void AddInboxPattern_WithNullConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInboxPattern(null);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<InboxOptions>>().Value;

        Assert.True(options.Enabled); // Default when no config provided
        Assert.Equal(TimeSpan.FromDays(7), options.RetentionPeriod);
        Assert.Equal(TimeSpan.FromHours(1), options.CleanupInterval);
    }

    [Fact]
    public void AddInboxPatternWithSql_WithValidConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInboxPatternWithSql(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var inboxStore = serviceProvider.GetService<IInboxStore>();
        var dbContextFactory = serviceProvider.GetService<IDbContextFactory<InboxDbContext>>();
        var hostedServices = serviceProvider.GetServices<IHostedService>();

        Assert.NotNull(inboxStore);
        Assert.IsType<SqlInboxStore>(inboxStore);
        Assert.NotNull(dbContextFactory);
        Assert.Contains(hostedServices, s => s is InboxCleanupWorker);
    }

    [Fact]
    public void AddInboxPatternWithSql_WithNullConfigureDbContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddInboxPatternWithSql(null!));
    }

    [Fact]
    public void AddInboxPatternWithSql_WithCustomConfiguration_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInboxPatternWithSql(
            options => options.UseInMemoryDatabase("TestDb"),
            inboxOptions =>
            {
                inboxOptions.Enabled = false;
                inboxOptions.RetentionPeriod = TimeSpan.FromDays(30);
                inboxOptions.ConsumerName = "SqlConsumer";
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<InboxOptions>>().Value;

        Assert.False(options.Enabled);
        Assert.Equal(TimeSpan.FromDays(30), options.RetentionPeriod);
        Assert.Equal("SqlConsumer", options.ConsumerName);
    }

    [Fact]
    public void DecorateMessageBrokerWithInbox_ShouldDecorateMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddInboxPattern(); // Need inbox store

        // Act
        services.DecorateMessageBrokerWithInbox();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var messageBroker = serviceProvider.GetService<IMessageBroker>();

        Assert.NotNull(messageBroker);
        Assert.IsType<InboxMessageBrokerDecorator>(messageBroker);
    }

    [Fact]
    public void DecorateMessageBrokerWithInbox_WithoutMessageBroker_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should throw because there's no message broker to decorate
        Assert.Throws<DecorationException>(() =>
            services.DecorateMessageBrokerWithInbox());
    }
}