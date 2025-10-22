using Relay.CLI.Commands.Models;

using Xunit;

namespace Relay.CLI.Tests.Commands;

public class HandlerInfoTests
{
    [Fact]
    public void HandlerInfo_ShouldHaveNameProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { Name = "CreateUserHandler" };

        // Assert
        Assert.Equal("CreateUserHandler", handler.Name);
    }

    [Fact]
    public void HandlerInfo_ShouldHaveFilePathProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { FilePath = "src/Handlers/CreateUserHandler.cs" };

        // Assert
        Assert.Equal("src/Handlers/CreateUserHandler.cs", handler.FilePath);
    }

    [Fact]
    public void HandlerInfo_ShouldHaveIsAsyncProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { IsAsync = true };

        // Assert
        Assert.True(handler.IsAsync);
    }

    [Fact]
    public void HandlerInfo_ShouldHaveHasDependenciesProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { HasDependencies = true };

        // Assert
        Assert.True(handler.HasDependencies);
    }

    [Fact]
    public void HandlerInfo_ShouldHaveUsesValueTaskProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { UsesValueTask = true };

        // Assert
        Assert.True(handler.UsesValueTask);
    }

    [Fact]
    public void HandlerInfo_ShouldHaveHasCancellationTokenProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { HasCancellationToken = true };

        // Assert
        Assert.True(handler.HasCancellationToken);
    }

    [Fact]
    public void HandlerInfo_ShouldHaveHasLoggingProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { HasLogging = true };

        // Assert
        Assert.True(handler.HasLogging);
    }

    [Fact]
    public void HandlerInfo_ShouldHaveHasValidationProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { HasValidation = true };

        // Assert
        Assert.True(handler.HasValidation);
    }

    [Fact]
    public void HandlerInfo_ShouldHaveLineCountProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { LineCount = 150 };

        // Assert
        Assert.Equal(150, handler.LineCount);
    }

    [Fact]
    public void HandlerInfo_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var handler = new HandlerInfo();

        // Assert
        Assert.Equal("", handler.Name);
        Assert.Equal("", handler.FilePath);
        Assert.False(handler.IsAsync);
        Assert.False(handler.HasDependencies);
        Assert.False(handler.UsesValueTask);
        Assert.False(handler.HasCancellationToken);
        Assert.False(handler.HasLogging);
        Assert.False(handler.HasValidation);
        Assert.Equal(0, handler.LineCount);
    }

    [Fact]
    public void HandlerInfo_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var handler = new HandlerInfo
        {
            Name = "UpdateUserHandler",
            FilePath = "src/Handlers/UpdateUserHandler.cs",
            IsAsync = true,
            HasDependencies = true,
            UsesValueTask = false,
            HasCancellationToken = true,
            HasLogging = true,
            HasValidation = false,
            LineCount = 200
        };

        // Assert
        Assert.Equal("UpdateUserHandler", handler.Name);
        Assert.Equal("src/Handlers/UpdateUserHandler.cs", handler.FilePath);
        Assert.True(handler.IsAsync);
        Assert.True(handler.HasDependencies);
        Assert.False(handler.UsesValueTask);
        Assert.True(handler.HasCancellationToken);
        Assert.True(handler.HasLogging);
        Assert.False(handler.HasValidation);
        Assert.Equal(200, handler.LineCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(1000)]
    public void HandlerInfo_ShouldSupportVariousLineCounts(int lineCount)
    {
        // Arrange & Act
        var handler = new HandlerInfo { LineCount = lineCount };

        // Assert
        Assert.Equal(lineCount, handler.LineCount);
    }

    [Fact]
    public void HandlerInfo_Name_CanBeEmpty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { Name = "" };

        // Assert
        Assert.Equal("", handler.Name);
    }

    [Fact]
    public void HandlerInfo_Name_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var handler = new HandlerInfo { Name = "CreateUserHandler_v2" };

        // Assert
        Assert.Equal("CreateUserHandler_v2", handler.Name);
    }

    [Fact]
    public void HandlerInfo_FilePath_CanBeEmpty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { FilePath = "" };

        // Assert
        Assert.Equal("", handler.FilePath);
    }

    [Fact]
    public void HandlerInfo_FilePath_CanContainPathSeparators()
    {
        // Arrange & Act
        var handler = new HandlerInfo { FilePath = "src/Handlers/User/CreateUserHandler.cs" };

        // Assert
        Assert.Equal("src/Handlers/User/CreateUserHandler.cs", handler.FilePath);
    }

    [Fact]
    public void HandlerInfo_BooleanProperties_CanBeTrueOrFalse()
    {
        // Test all boolean properties can be set to true
        var handlerTrue = new HandlerInfo
        {
            IsAsync = true,
            HasDependencies = true,
            UsesValueTask = true,
            HasCancellationToken = true,
            HasLogging = true,
            HasValidation = true
        };

        Assert.True(handlerTrue.IsAsync);
        Assert.True(handlerTrue.HasDependencies);
        Assert.True(handlerTrue.UsesValueTask);
        Assert.True(handlerTrue.HasCancellationToken);
        Assert.True(handlerTrue.HasLogging);
        Assert.True(handlerTrue.HasValidation);

        // Test all boolean properties can be set to false
        var handlerFalse = new HandlerInfo
        {
            IsAsync = false,
            HasDependencies = false,
            UsesValueTask = false,
            HasCancellationToken = false,
            HasLogging = false,
            HasValidation = false
        };

        Assert.False(handlerFalse.IsAsync);
        Assert.False(handlerFalse.HasDependencies);
        Assert.False(handlerFalse.UsesValueTask);
        Assert.False(handlerFalse.HasCancellationToken);
        Assert.False(handlerFalse.HasLogging);
        Assert.False(handlerFalse.HasValidation);
    }

    [Fact]
    public void HandlerInfo_CanBeUsedInCollections()
    {
        // Arrange & Act
        var handlers = new List<HandlerInfo>
        {
            new() { Name = "CreateUserHandler", IsAsync = true, LineCount = 100 },
            new() { Name = "UpdateUserHandler", IsAsync = false, LineCount = 80 },
            new() { Name = "DeleteUserHandler", IsAsync = true, LineCount = 60 }
        };

        // Assert
        Assert.Equal(3, handlers.Count);
        Assert.Equal(2, handlers.Count(h => h.IsAsync));
        Assert.Equal(240, handlers.Sum(h => h.LineCount));
    }

    [Fact]
    public void HandlerInfo_CanBeFilteredByAsync()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new() { Name = "Handler1", IsAsync = true },
            new() { Name = "Handler2", IsAsync = false },
            new() { Name = "Handler3", IsAsync = true },
            new() { Name = "Handler4", IsAsync = false }
        };

        // Act
        var asyncHandlers = handlers.Where(h => h.IsAsync).ToList();
        var syncHandlers = handlers.Where(h => !h.IsAsync).ToList();

        // Assert
        Assert.Equal(2, asyncHandlers.Count);
        Assert.Equal(2, syncHandlers.Count);
        Assert.True(asyncHandlers.All(h => h.IsAsync));
        Assert.True(syncHandlers.All(h => !h.IsAsync));
    }

    [Fact]
    public void HandlerInfo_CanBeFilteredByFeatures()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new() { Name = "Handler1", HasLogging = true, HasValidation = true },
            new() { Name = "Handler2", HasLogging = false, HasValidation = true },
            new() { Name = "Handler3", HasLogging = true, HasValidation = false },
            new() { Name = "Handler4", HasLogging = false, HasValidation = false }
        };

        // Act
        var handlersWithLogging = handlers.Where(h => h.HasLogging).ToList();
        var handlersWithValidation = handlers.Where(h => h.HasValidation).ToList();
        var handlersWithBoth = handlers.Where(h => h.HasLogging && h.HasValidation).ToList();

        // Assert
        Assert.Equal(2, handlersWithLogging.Count);
        Assert.Equal(2, handlersWithValidation.Count);
        Assert.Single(handlersWithBoth);
    }

    [Fact]
    public void HandlerInfo_CanBeOrderedByLineCount()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new() { Name = "LargeHandler", LineCount = 500 },
            new() { Name = "SmallHandler", LineCount = 50 },
            new() { Name = "MediumHandler", LineCount = 200 }
        };

        // Act
        var orderedBySize = handlers.OrderBy(h => h.LineCount).ToList();

        // Assert
        Assert.Equal("SmallHandler", orderedBySize[0].Name);
        Assert.Equal("MediumHandler", orderedBySize[1].Name);
        Assert.Equal("LargeHandler", orderedBySize[2].Name);
    }

    [Fact]
    public void HandlerInfo_CanBeGroupedByAsync()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new() { Name = "Async1", IsAsync = true, LineCount = 100 },
            new() { Name = "Sync1", IsAsync = false, LineCount = 80 },
            new() { Name = "Async2", IsAsync = true, LineCount = 120 },
            new() { Name = "Sync2", IsAsync = false, LineCount = 90 }
        };

        // Act
        var grouped = handlers.GroupBy(h => h.IsAsync);

        // Assert
        Assert.Equal(2, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key).Count()); // Async
        Assert.Equal(2, grouped.First(g => !g.Key).Count()); // Sync

        var asyncGroup = grouped.First(g => g.Key);
        Assert.Equal(220, asyncGroup.Sum(h => h.LineCount));
    }

    [Fact]
    public void HandlerInfo_PropertiesCanBeModified()
    {
        // Arrange
        var handler = new HandlerInfo
        {
            Name = "InitialHandler",
            FilePath = "initial.cs",
            IsAsync = false,
            LineCount = 100
        };

        // Act
        handler.Name = "ModifiedHandler";
        handler.FilePath = "modified.cs";
        handler.IsAsync = true;
        handler.LineCount = 150;

        // Assert
        Assert.Equal("ModifiedHandler", handler.Name);
        Assert.Equal("modified.cs", handler.FilePath);
        Assert.True(handler.IsAsync);
        Assert.Equal(150, handler.LineCount);
    }

    [Fact]
    public void HandlerInfo_ShouldBeClass()
    {
        // Arrange & Act
        var handler = new HandlerInfo();

        // Assert
        Assert.NotNull(handler);
        Assert.True(handler.GetType().IsClass);
    }

    [Fact]
    public void HandlerInfo_WithRealisticData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var handler = new HandlerInfo
        {
            Name = "CreateUserCommandHandler",
            FilePath = "src/Application/Users/Commands/CreateUser/CreateUserCommandHandler.cs",
            IsAsync = true,
            HasDependencies = true,
            UsesValueTask = false,
            HasCancellationToken = true,
            HasLogging = true,
            HasValidation = true,
            LineCount = 87
        };

        // Assert
        Assert.Equal("CreateUserCommandHandler", handler.Name);
        Assert.Contains("CreateUserCommandHandler.cs", handler.FilePath);
        Assert.True(handler.IsAsync);
        Assert.True(handler.HasDependencies);
        Assert.True(handler.HasCancellationToken);
        Assert.True(handler.HasLogging);
        Assert.True(handler.HasValidation);
        Assert.Equal(87, handler.LineCount);
    }

    [Fact]
    public void HandlerInfo_WithSimpleHandlerData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var handler = new HandlerInfo
        {
            Name = "GetUserQueryHandler",
            FilePath = "src/Application/Users/Queries/GetUserQueryHandler.cs",
            IsAsync = true,
            HasDependencies = false,
            UsesValueTask = false,
            HasCancellationToken = false,
            HasLogging = false,
            HasValidation = false,
            LineCount = 25
        };

        // Assert
        Assert.Equal("GetUserQueryHandler", handler.Name);
        Assert.True(handler.IsAsync);
        Assert.False(handler.HasDependencies);
        Assert.False(handler.HasCancellationToken);
        Assert.False(handler.HasLogging);
        Assert.False(handler.HasValidation);
        Assert.Equal(25, handler.LineCount);
    }

    [Fact]
    public void HandlerInfo_CanCalculateStatistics()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new() { Name = "H1", IsAsync = true, HasLogging = true, LineCount = 100 },
            new() { Name = "H2", IsAsync = false, HasLogging = true, LineCount = 80 },
            new() { Name = "H3", IsAsync = true, HasLogging = false, LineCount = 120 },
            new() { Name = "H4", IsAsync = true, HasLogging = true, LineCount = 90 }
        };

        // Act
        var totalLines = handlers.Sum(h => h.LineCount);
        var asyncHandlers = handlers.Count(h => h.IsAsync);
        var handlersWithLogging = handlers.Count(h => h.HasLogging);
        var averageLines = handlers.Average(h => h.LineCount);

        // Assert
        Assert.Equal(390, totalLines);
        Assert.Equal(3, asyncHandlers);
        Assert.Equal(3, handlersWithLogging);
        Assert.Equal(97.5, averageLines);
    }

    [Fact]
    public void HandlerInfo_CanBeFilteredByFilePath()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new() { Name = "UserHandler", FilePath = "src/Users/Commands/CreateUserHandler.cs" },
            new() { Name = "ProductHandler", FilePath = "src/Products/Commands/CreateProductHandler.cs" },
            new() { Name = "OrderHandler", FilePath = "src/Orders/Commands/CreateOrderHandler.cs" }
        };

        // Act
        var userHandlers = handlers.Where(h => h.FilePath.Contains("Users")).ToList();
        var commandHandlers = handlers.Where(h => h.FilePath.Contains("Commands")).ToList();

        // Assert
        Assert.Single(userHandlers);
        Assert.Equal(3, commandHandlers.Count);
    }

    [Fact]
    public void HandlerInfo_CanIdentifyComplexHandlers()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new() { Name = "SimpleHandler", HasDependencies = false, HasLogging = false, HasValidation = false, LineCount = 20 },
            new() { Name = "ComplexHandler", HasDependencies = true, HasLogging = true, HasValidation = true, LineCount = 200 },
            new() { Name = "MediumHandler", HasDependencies = true, HasLogging = false, HasValidation = true, LineCount = 80 }
        };

        // Act
        var complexHandlers = handlers.Where(h => h.HasDependencies && h.HasLogging && h.HasValidation).ToList();
        var simpleHandlers = handlers.Where(h => !h.HasDependencies && !h.HasLogging && !h.HasValidation).ToList();

        // Assert
        Assert.Single(complexHandlers);
        Assert.Equal("ComplexHandler", complexHandlers[0].Name);
        Assert.Single(simpleHandlers);
        Assert.Equal("SimpleHandler", simpleHandlers[0].Name);
    }

    [Fact]
    public void HandlerInfo_LineCount_CanBeZero()
    {
        // Arrange & Act
        var handler = new HandlerInfo { LineCount = 0 };

        // Assert
        Assert.Equal(0, handler.LineCount);
    }

    [Fact]
    public void HandlerInfo_LineCount_CanBeLarge()
    {
        // Arrange & Act
        var handler = new HandlerInfo { LineCount = int.MaxValue };

        // Assert
        Assert.Equal(int.MaxValue, handler.LineCount);
    }

    [Fact]
    public void HandlerInfo_CanBeUsedInReporting()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new() {
                Name = "CreateUserHandler",
                FilePath = "src/Users/CreateUserHandler.cs",
                IsAsync = true,
                HasDependencies = true,
                HasLogging = true,
                HasValidation = true,
                LineCount = 150
            },
            new() {
                Name = "GetUserHandler",
                FilePath = "src/Users/GetUserHandler.cs",
                IsAsync = true,
                HasDependencies = false,
                HasLogging = false,
                HasValidation = false,
                LineCount = 30
            }
        };

        // Act - Simulate report generation
        var report = handlers.Select(h => new
        {
            HandlerName = h.Name,
            Complexity = (h.HasDependencies ? 1 : 0) + (h.HasLogging ? 1 : 0) + (h.HasValidation ? 1 : 0),
            Size = h.LineCount > 100 ? "Large" : h.LineCount > 50 ? "Medium" : "Small",
            Features = $"{(h.IsAsync ? "Async" : "Sync")}, {(h.HasDependencies ? "DI" : "No DI")}"
        }).ToList();

        // Assert
        Assert.Equal(3, report[0].Complexity);
        Assert.Equal("Large", report[0].Size);
        Assert.Contains("Async", report[0].Features);
        Assert.Contains("DI", report[0].Features);

        Assert.Equal(0, report[1].Complexity);
        Assert.Equal("Small", report[1].Size);
        Assert.Contains("Async", report[1].Features);
        Assert.Contains("No DI", report[1].Features);
    }

    [Fact]
    public void HandlerInfo_CanBeSerialized_WithComplexData()
    {
        // Arrange & Act
        var handler = new HandlerInfo
        {
            Name = "ComplexBusinessLogicHandler",
            FilePath = "src/Application/Business/Handlers/ComplexBusinessLogicHandler.cs",
            IsAsync = true,
            HasDependencies = true,
            UsesValueTask = false,
            HasCancellationToken = true,
            HasLogging = true,
            HasValidation = true,
            LineCount = 342
        };

        // Assert - Basic serialization check
        Assert.Equal("ComplexBusinessLogicHandler", handler.Name);
        Assert.Contains("ComplexBusinessLogicHandler.cs", handler.FilePath);
        Assert.True(handler.IsAsync);
        Assert.True(handler.HasDependencies);
        Assert.True(handler.HasCancellationToken);
        Assert.True(handler.HasLogging);
        Assert.True(handler.HasValidation);
        Assert.Equal(342, handler.LineCount);
    }
}
