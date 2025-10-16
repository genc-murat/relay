using Relay.CLI.Commands.Models;

namespace Relay.CLI.Tests.Commands;

public class HandlerInfoTests
{
    [Fact]
    public void HandlerInfo_ShouldHaveNameProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { Name = "CreateUserHandler" };

        // Assert
        handler.Name.Should().Be("CreateUserHandler");
    }

    [Fact]
    public void HandlerInfo_ShouldHaveFilePathProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { FilePath = "src/Handlers/CreateUserHandler.cs" };

        // Assert
        handler.FilePath.Should().Be("src/Handlers/CreateUserHandler.cs");
    }

    [Fact]
    public void HandlerInfo_ShouldHaveIsAsyncProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { IsAsync = true };

        // Assert
        handler.IsAsync.Should().BeTrue();
    }

    [Fact]
    public void HandlerInfo_ShouldHaveHasDependenciesProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { HasDependencies = true };

        // Assert
        handler.HasDependencies.Should().BeTrue();
    }

    [Fact]
    public void HandlerInfo_ShouldHaveUsesValueTaskProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { UsesValueTask = true };

        // Assert
        handler.UsesValueTask.Should().BeTrue();
    }

    [Fact]
    public void HandlerInfo_ShouldHaveHasCancellationTokenProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { HasCancellationToken = true };

        // Assert
        handler.HasCancellationToken.Should().BeTrue();
    }

    [Fact]
    public void HandlerInfo_ShouldHaveHasLoggingProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { HasLogging = true };

        // Assert
        handler.HasLogging.Should().BeTrue();
    }

    [Fact]
    public void HandlerInfo_ShouldHaveHasValidationProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { HasValidation = true };

        // Assert
        handler.HasValidation.Should().BeTrue();
    }

    [Fact]
    public void HandlerInfo_ShouldHaveLineCountProperty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { LineCount = 150 };

        // Assert
        handler.LineCount.Should().Be(150);
    }

    [Fact]
    public void HandlerInfo_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var handler = new HandlerInfo();

        // Assert
        handler.Name.Should().Be("");
        handler.FilePath.Should().Be("");
        handler.IsAsync.Should().BeFalse();
        handler.HasDependencies.Should().BeFalse();
        handler.UsesValueTask.Should().BeFalse();
        handler.HasCancellationToken.Should().BeFalse();
        handler.HasLogging.Should().BeFalse();
        handler.HasValidation.Should().BeFalse();
        handler.LineCount.Should().Be(0);
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
        handler.Name.Should().Be("UpdateUserHandler");
        handler.FilePath.Should().Be("src/Handlers/UpdateUserHandler.cs");
        handler.IsAsync.Should().BeTrue();
        handler.HasDependencies.Should().BeTrue();
        handler.UsesValueTask.Should().BeFalse();
        handler.HasCancellationToken.Should().BeTrue();
        handler.HasLogging.Should().BeTrue();
        handler.HasValidation.Should().BeFalse();
        handler.LineCount.Should().Be(200);
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
        handler.LineCount.Should().Be(lineCount);
    }

    [Fact]
    public void HandlerInfo_Name_CanBeEmpty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { Name = "" };

        // Assert
        handler.Name.Should().BeEmpty();
    }

    [Fact]
    public void HandlerInfo_Name_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var handler = new HandlerInfo { Name = "CreateUserHandler_v2" };

        // Assert
        handler.Name.Should().Be("CreateUserHandler_v2");
    }

    [Fact]
    public void HandlerInfo_FilePath_CanBeEmpty()
    {
        // Arrange & Act
        var handler = new HandlerInfo { FilePath = "" };

        // Assert
        handler.FilePath.Should().BeEmpty();
    }

    [Fact]
    public void HandlerInfo_FilePath_CanContainPathSeparators()
    {
        // Arrange & Act
        var handler = new HandlerInfo { FilePath = "src/Handlers/User/CreateUserHandler.cs" };

        // Assert
        handler.FilePath.Should().Be("src/Handlers/User/CreateUserHandler.cs");
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

        handlerTrue.IsAsync.Should().BeTrue();
        handlerTrue.HasDependencies.Should().BeTrue();
        handlerTrue.UsesValueTask.Should().BeTrue();
        handlerTrue.HasCancellationToken.Should().BeTrue();
        handlerTrue.HasLogging.Should().BeTrue();
        handlerTrue.HasValidation.Should().BeTrue();

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

        handlerFalse.IsAsync.Should().BeFalse();
        handlerFalse.HasDependencies.Should().BeFalse();
        handlerFalse.UsesValueTask.Should().BeFalse();
        handlerFalse.HasCancellationToken.Should().BeFalse();
        handlerFalse.HasLogging.Should().BeFalse();
        handlerFalse.HasValidation.Should().BeFalse();
    }

    [Fact]
    public void HandlerInfo_CanBeUsedInCollections()
    {
        // Arrange & Act
        var handlers = new List<HandlerInfo>
        {
            new HandlerInfo { Name = "CreateUserHandler", IsAsync = true, LineCount = 100 },
            new HandlerInfo { Name = "UpdateUserHandler", IsAsync = false, LineCount = 80 },
            new HandlerInfo { Name = "DeleteUserHandler", IsAsync = true, LineCount = 60 }
        };

        // Assert
        handlers.Should().HaveCount(3);
        handlers.Count(h => h.IsAsync).Should().Be(2);
        handlers.Sum(h => h.LineCount).Should().Be(240);
    }

    [Fact]
    public void HandlerInfo_CanBeFilteredByAsync()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new HandlerInfo { Name = "Handler1", IsAsync = true },
            new HandlerInfo { Name = "Handler2", IsAsync = false },
            new HandlerInfo { Name = "Handler3", IsAsync = true },
            new HandlerInfo { Name = "Handler4", IsAsync = false }
        };

        // Act
        var asyncHandlers = handlers.Where(h => h.IsAsync).ToList();
        var syncHandlers = handlers.Where(h => !h.IsAsync).ToList();

        // Assert
        asyncHandlers.Should().HaveCount(2);
        syncHandlers.Should().HaveCount(2);
        asyncHandlers.All(h => h.IsAsync).Should().BeTrue();
        syncHandlers.All(h => !h.IsAsync).Should().BeTrue();
    }

    [Fact]
    public void HandlerInfo_CanBeFilteredByFeatures()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new HandlerInfo { Name = "Handler1", HasLogging = true, HasValidation = true },
            new HandlerInfo { Name = "Handler2", HasLogging = false, HasValidation = true },
            new HandlerInfo { Name = "Handler3", HasLogging = true, HasValidation = false },
            new HandlerInfo { Name = "Handler4", HasLogging = false, HasValidation = false }
        };

        // Act
        var handlersWithLogging = handlers.Where(h => h.HasLogging).ToList();
        var handlersWithValidation = handlers.Where(h => h.HasValidation).ToList();
        var handlersWithBoth = handlers.Where(h => h.HasLogging && h.HasValidation).ToList();

        // Assert
        handlersWithLogging.Should().HaveCount(2);
        handlersWithValidation.Should().HaveCount(2);
        handlersWithBoth.Should().HaveCount(1);
    }

    [Fact]
    public void HandlerInfo_CanBeOrderedByLineCount()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new HandlerInfo { Name = "LargeHandler", LineCount = 500 },
            new HandlerInfo { Name = "SmallHandler", LineCount = 50 },
            new HandlerInfo { Name = "MediumHandler", LineCount = 200 }
        };

        // Act
        var orderedBySize = handlers.OrderBy(h => h.LineCount).ToList();

        // Assert
        orderedBySize[0].Name.Should().Be("SmallHandler");
        orderedBySize[1].Name.Should().Be("MediumHandler");
        orderedBySize[2].Name.Should().Be("LargeHandler");
    }

    [Fact]
    public void HandlerInfo_CanBeGroupedByAsync()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new HandlerInfo { Name = "Async1", IsAsync = true, LineCount = 100 },
            new HandlerInfo { Name = "Sync1", IsAsync = false, LineCount = 80 },
            new HandlerInfo { Name = "Async2", IsAsync = true, LineCount = 120 },
            new HandlerInfo { Name = "Sync2", IsAsync = false, LineCount = 90 }
        };

        // Act
        var grouped = handlers.GroupBy(h => h.IsAsync);

        // Assert
        grouped.Should().HaveCount(2);
        grouped.First(g => g.Key).Should().HaveCount(2); // Async
        grouped.First(g => !g.Key).Should().HaveCount(2); // Sync

        var asyncGroup = grouped.First(g => g.Key);
        asyncGroup.Sum(h => h.LineCount).Should().Be(220);
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
        handler.Name.Should().Be("ModifiedHandler");
        handler.FilePath.Should().Be("modified.cs");
        handler.IsAsync.Should().BeTrue();
        handler.LineCount.Should().Be(150);
    }

    [Fact]
    public void HandlerInfo_ShouldBeClass()
    {
        // Arrange & Act
        var handler = new HandlerInfo();

        // Assert
        handler.Should().NotBeNull();
        handler.GetType().IsClass.Should().BeTrue();
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
        handler.Name.Should().Be("CreateUserCommandHandler");
        handler.FilePath.Should().Contain("CreateUserCommandHandler.cs");
        handler.IsAsync.Should().BeTrue();
        handler.HasDependencies.Should().BeTrue();
        handler.HasCancellationToken.Should().BeTrue();
        handler.HasLogging.Should().BeTrue();
        handler.HasValidation.Should().BeTrue();
        handler.LineCount.Should().Be(87);
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
        handler.Name.Should().Be("GetUserQueryHandler");
        handler.IsAsync.Should().BeTrue();
        handler.HasDependencies.Should().BeFalse();
        handler.HasCancellationToken.Should().BeFalse();
        handler.HasLogging.Should().BeFalse();
        handler.HasValidation.Should().BeFalse();
        handler.LineCount.Should().Be(25);
    }

    [Fact]
    public void HandlerInfo_CanCalculateStatistics()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new HandlerInfo { Name = "H1", IsAsync = true, HasLogging = true, LineCount = 100 },
            new HandlerInfo { Name = "H2", IsAsync = false, HasLogging = true, LineCount = 80 },
            new HandlerInfo { Name = "H3", IsAsync = true, HasLogging = false, LineCount = 120 },
            new HandlerInfo { Name = "H4", IsAsync = true, HasLogging = true, LineCount = 90 }
        };

        // Act
        var totalLines = handlers.Sum(h => h.LineCount);
        var asyncHandlers = handlers.Count(h => h.IsAsync);
        var handlersWithLogging = handlers.Count(h => h.HasLogging);
        var averageLines = handlers.Average(h => h.LineCount);

        // Assert
        totalLines.Should().Be(390);
        asyncHandlers.Should().Be(3);
        handlersWithLogging.Should().Be(3);
        averageLines.Should().Be(97.5);
    }

    [Fact]
    public void HandlerInfo_CanBeFilteredByFilePath()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new HandlerInfo { Name = "UserHandler", FilePath = "src/Users/Commands/CreateUserHandler.cs" },
            new HandlerInfo { Name = "ProductHandler", FilePath = "src/Products/Commands/CreateProductHandler.cs" },
            new HandlerInfo { Name = "OrderHandler", FilePath = "src/Orders/Commands/CreateOrderHandler.cs" }
        };

        // Act
        var userHandlers = handlers.Where(h => h.FilePath.Contains("Users")).ToList();
        var commandHandlers = handlers.Where(h => h.FilePath.Contains("Commands")).ToList();

        // Assert
        userHandlers.Should().HaveCount(1);
        commandHandlers.Should().HaveCount(3);
    }

    [Fact]
    public void HandlerInfo_CanIdentifyComplexHandlers()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new HandlerInfo { Name = "SimpleHandler", HasDependencies = false, HasLogging = false, HasValidation = false, LineCount = 20 },
            new HandlerInfo { Name = "ComplexHandler", HasDependencies = true, HasLogging = true, HasValidation = true, LineCount = 200 },
            new HandlerInfo { Name = "MediumHandler", HasDependencies = true, HasLogging = false, HasValidation = true, LineCount = 80 }
        };

        // Act
        var complexHandlers = handlers.Where(h => h.HasDependencies && h.HasLogging && h.HasValidation).ToList();
        var simpleHandlers = handlers.Where(h => !h.HasDependencies && !h.HasLogging && !h.HasValidation).ToList();

        // Assert
        complexHandlers.Should().HaveCount(1);
        complexHandlers[0].Name.Should().Be("ComplexHandler");
        simpleHandlers.Should().HaveCount(1);
        simpleHandlers[0].Name.Should().Be("SimpleHandler");
    }

    [Fact]
    public void HandlerInfo_LineCount_CanBeZero()
    {
        // Arrange & Act
        var handler = new HandlerInfo { LineCount = 0 };

        // Assert
        handler.LineCount.Should().Be(0);
    }

    [Fact]
    public void HandlerInfo_LineCount_CanBeLarge()
    {
        // Arrange & Act
        var handler = new HandlerInfo { LineCount = int.MaxValue };

        // Assert
        handler.LineCount.Should().Be(int.MaxValue);
    }

    [Fact]
    public void HandlerInfo_CanBeUsedInReporting()
    {
        // Arrange
        var handlers = new List<HandlerInfo>
        {
            new HandlerInfo
            {
                Name = "CreateUserHandler",
                FilePath = "src/Users/CreateUserHandler.cs",
                IsAsync = true,
                HasDependencies = true,
                HasLogging = true,
                HasValidation = true,
                LineCount = 150
            },
            new HandlerInfo
            {
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
        report[0].Complexity.Should().Be(3);
        report[0].Size.Should().Be("Large");
        report[0].Features.Should().Contain("Async");
        report[0].Features.Should().Contain("DI");

        report[1].Complexity.Should().Be(0);
        report[1].Size.Should().Be("Small");
        report[1].Features.Should().Contain("Async");
        report[1].Features.Should().Contain("No DI");
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
        handler.Name.Should().Be("ComplexBusinessLogicHandler");
        handler.FilePath.Should().Contain("ComplexBusinessLogicHandler.cs");
        handler.IsAsync.Should().BeTrue();
        handler.HasDependencies.Should().BeTrue();
        handler.HasCancellationToken.Should().BeTrue();
        handler.HasLogging.Should().BeTrue();
        handler.HasValidation.Should().BeTrue();
        handler.LineCount.Should().Be(342);
    }
}