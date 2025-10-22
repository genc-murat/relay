
using Relay.CLI.Commands;
using Relay.CLI.Commands.Models.Optimization;
using Xunit;

namespace Relay.CLI.Tests.Commands;

/// <summary>
/// Comprehensive tests for OptimizationAction class
/// </summary>
public class OptimizationActionTests
{
    #region Constructor and Default Values Tests

    [Fact]
    public void OptimizationAction_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var action = new OptimizationAction();

        // Assert
        Assert.Equal("", action.FilePath);
        Assert.Equal("", action.Type);
        Assert.NotNull(action.Modifications);
        Assert.Empty(action.Modifications);
        Assert.Equal("", action.OriginalContent);
        Assert.Equal("", action.OptimizedContent);
    }

    [Fact]
    public void OptimizationAction_NewInstance_ShouldHaveEmptyModificationsList()
    {
        // Act
        var action = new OptimizationAction();

        // Assert
        Assert.NotNull(action.Modifications);
        Assert.Empty(action.Modifications);
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void OptimizationAction_FilePath_ShouldBeSettable()
    {
        // Arrange
        var action = new OptimizationAction();
        var expectedPath = @"C:\Projects\MyProject\Handler.cs";

        // Act
        action.FilePath = expectedPath;

        // Assert
        Assert.Equal(expectedPath, action.FilePath);
    }

    [Fact]
    public void OptimizationAction_Type_ShouldBeSettable()
    {
        // Arrange
        var action = new OptimizationAction();
        var expectedType = "Handler";

        // Act
        action.Type = expectedType;

        // Assert
        Assert.Equal(expectedType, action.Type);
    }

    [Fact]
    public void OptimizationAction_Modifications_ShouldBeSettable()
    {
        // Arrange
        var action = new OptimizationAction();
        var modifications = new List<string> { "Task -> ValueTask", "Added [Handle]" };

        // Act
        action.Modifications = modifications;

        // Assert
        Assert.Equal(modifications, action.Modifications);
        Assert.Equal(2, action.Modifications.Count());
    }

    [Fact]
    public void OptimizationAction_OriginalContent_ShouldBeSettable()
    {
        // Arrange
        var action = new OptimizationAction();
        var originalContent = "public async Task<Result> Handle()";

        // Act
        action.OriginalContent = originalContent;

        // Assert
        Assert.Equal(originalContent, action.OriginalContent);
    }

    [Fact]
    public void OptimizationAction_OptimizedContent_ShouldBeSettable()
    {
        // Arrange
        var action = new OptimizationAction();
        var optimizedContent = "public async ValueTask<Result> HandleAsync()";

        // Act
        action.OptimizedContent = optimizedContent;

        // Assert
        Assert.Equal(optimizedContent, action.OptimizedContent);
    }

    #endregion

    #region Complete Action Tests

    [Fact]
    public void OptimizationAction_WithAllProperties_ShouldStoreAllValues()
    {
        // Arrange & Act
        var action = new OptimizationAction
        {
            FilePath = @"C:\Projects\Handler.cs",
            Type = "Handler",
            Modifications = new List<string> { "Task -> ValueTask" },
            OriginalContent = "public async Task<Result> Handle()",
            OptimizedContent = "public async ValueTask<Result> HandleAsync()"
        };

        // Assert
        Assert.Equal(@"C:\Projects\Handler.cs", action.FilePath);
        Assert.Equal("Handler", action.Type);
        Assert.Single(action.Modifications);
        Assert.NotEmpty(action.OriginalContent);
        Assert.NotEmpty(action.OptimizedContent);
    }

    #endregion

    #region Modification Management Tests

    [Fact]
    public void OptimizationAction_Modifications_CanAddSingleItem()
    {
        // Arrange
        var action = new OptimizationAction();

        // Act
        action.Modifications.Add("Task -> ValueTask");

        // Assert
        Assert.Single(action.Modifications);
        Assert.Contains("Task -> ValueTask", action.Modifications);
    }

    [Fact]
    public void OptimizationAction_Modifications_CanAddMultipleItems()
    {
        // Arrange
        var action = new OptimizationAction();

        // Act
        action.Modifications.Add("Task -> ValueTask");
        action.Modifications.Add("Added [Handle] attribute");
        action.Modifications.Add("Handle -> HandleAsync");

        // Assert
        Assert.Equal(3, action.Modifications.Count());
    }

    [Fact]
    public void OptimizationAction_Modifications_CanBeCleared()
    {
        // Arrange
        var action = new OptimizationAction
        {
            Modifications = new List<string> { "Mod1", "Mod2" }
        };

        // Act
        action.Modifications.Clear();

        // Assert
        Assert.Empty(action.Modifications);
    }

    [Fact]
    public void OptimizationAction_Modifications_CanCheckContains()
    {
        // Arrange
        var action = new OptimizationAction();
        action.Modifications.Add("Task -> ValueTask");

        // Act
        var contains = action.Modifications.Contains("Task -> ValueTask");

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void OptimizationAction_Modifications_CanRemoveItem()
    {
        // Arrange
        var action = new OptimizationAction();
        action.Modifications.Add("Task -> ValueTask");
        action.Modifications.Add("Added [Handle]");

        // Act
        action.Modifications.Remove("Task -> ValueTask");

        // Assert
        Assert.Single(action.Modifications);
        Assert.DoesNotContain("Task -> ValueTask", action.Modifications);
    }

    #endregion

    #region Type Classification Tests

    [Theory]
    [InlineData("Handler")]
    [InlineData("Request")]
    [InlineData("Response")]
    [InlineData("Behavior")]
    [InlineData("Validator")]
    public void OptimizationAction_Type_ShouldSupportCommonTypes(string type)
    {
        // Arrange & Act
        var action = new OptimizationAction { Type = type };

        // Assert
        Assert.Equal(type, action.Type);
    }

    [Fact]
    public void OptimizationAction_Type_Handler_ShouldBeRecognized()
    {
        // Arrange & Act
        var action = new OptimizationAction { Type = "Handler" };

        // Assert
        Assert.Equal("Handler", action.Type);
    }

    [Fact]
    public void OptimizationAction_Type_Request_ShouldBeRecognized()
    {
        // Arrange & Act
        var action = new OptimizationAction { Type = "Request" };

        // Assert
        Assert.Equal("Request", action.Type);
    }

    [Fact]
    public void OptimizationAction_Type_Config_ShouldBeRecognized()
    {
        // Arrange & Act
        var action = new OptimizationAction { Type = "Config" };

        // Assert
        Assert.Equal("Config", action.Type);
    }

    #endregion

    #region File Path Tests

    [Fact]
    public void OptimizationAction_FilePath_ShouldSupportAbsolutePath()
    {
        // Arrange & Act
        var absolutePath = Path.IsPathRooted("/tmp") ? "/tmp/Relay/src/Handlers/GetUserHandler.cs" : @"C:\Projects\Relay\src\Handlers\GetUserHandler.cs";
        var action = new OptimizationAction
        {
            FilePath = absolutePath
        };

        // Assert
        Assert.Contains("GetUserHandler.cs", action.FilePath);
        Assert.True(Path.IsPathRooted(action.FilePath));
    }

    [Fact]
    public void OptimizationAction_FilePath_ShouldSupportRelativePath()
    {
        // Arrange & Act
        var action = new OptimizationAction
        {
            FilePath = @"src\Handlers\GetUserHandler.cs"
        };

        // Assert
        Assert.Contains("GetUserHandler.cs", action.FilePath);
    }

    [Fact]
    public void OptimizationAction_FilePath_ShouldSupportUnixPath()
    {
        // Arrange & Act
        var action = new OptimizationAction
        {
            FilePath = "/home/project/src/Handlers/GetUserHandler.cs"
        };

        // Assert
        Assert.Contains("GetUserHandler.cs", action.FilePath);
    }

    [Fact]
    public void OptimizationAction_FilePath_CanExtractFileName()
    {
        // Arrange
        var absolutePath = Path.IsPathRooted("/tmp") ? "/tmp/Handler.cs" : @"C:\Projects\Handler.cs";
        var action = new OptimizationAction
        {
            FilePath = absolutePath
        };

        // Act
        var fileName = Path.GetFileName(action.FilePath);

        // Assert
        Assert.Equal("Handler.cs", fileName);
    }

    [Fact]
    public void OptimizationAction_FilePath_CanExtractDirectory()
    {
        // Arrange
        var absolutePath = Path.IsPathRooted("/tmp") ? "/tmp/Handlers/UserHandler.cs" : @"C:\Projects\Handlers\UserHandler.cs";
        var action = new OptimizationAction
        {
            FilePath = absolutePath
        };

        // Act
        var directory = Path.GetDirectoryName(action.FilePath);

        // Assert
        Assert.Contains("Handlers", directory);
    }

    #endregion

    #region Content Comparison Tests

    [Fact]
    public void OptimizationAction_ContentDifference_ShouldBeDetectable()
    {
        // Arrange
        var action = new OptimizationAction
        {
            OriginalContent = "public async Task<Result> Handle()",
            OptimizedContent = "public async ValueTask<Result> HandleAsync()"
        };

        // Act
        var isDifferent = action.OriginalContent != action.OptimizedContent;

        // Assert
        Assert.True(isDifferent);
    }

    [Fact]
    public void OptimizationAction_SameContent_ShouldBeEqual()
    {
        // Arrange
        var content = "public async Task<Result> Handle()";
        var action = new OptimizationAction
        {
            OriginalContent = content,
            OptimizedContent = content
        };

        // Act
        var isSame = action.OriginalContent == action.OptimizedContent;

        // Assert
        Assert.True(isSame);
    }

    [Fact]
    public void OptimizationAction_ContentLength_ShouldBeAccessible()
    {
        // Arrange
        var action = new OptimizationAction
        {
            OriginalContent = "short",
            OptimizedContent = "much longer optimized content"
        };

        // Assert
        Assert.True(action.OriginalContent.Length < action.OptimizedContent.Length);
    }

    #endregion

    #region Modification Description Tests

    [Fact]
    public void OptimizationAction_Modification_TaskToValueTask()
    {
        // Arrange & Act
        var action = new OptimizationAction();
        action.Modifications.Add("Replaced Task<T> with ValueTask<T>");

        // Assert
        Assert.Contains(action.Modifications, m => m.Contains("ValueTask"));
    }

    [Fact]
    public void OptimizationAction_Modification_HandleAttribute()
    {
        // Arrange & Act
        var action = new OptimizationAction();
        action.Modifications.Add("Added [Handle] attribute");

        // Assert
        Assert.Contains(action.Modifications, m => m.Contains("[Handle]"));
    }

    [Fact]
    public void OptimizationAction_Modification_MethodRename()
    {
        // Arrange & Act
        var action = new OptimizationAction();
        action.Modifications.Add("Renamed Handle() to HandleAsync()");

        // Assert
        Assert.Contains(action.Modifications, m => m.Contains("HandleAsync"));
    }

    [Fact]
    public void OptimizationAction_Modification_CancellationToken()
    {
        // Arrange & Act
        var action = new OptimizationAction();
        action.Modifications.Add("Added CancellationToken parameter");

        // Assert
        Assert.Contains(action.Modifications, m => m.Contains("CancellationToken"));
    }

    [Fact]
    public void OptimizationAction_Modification_ConfigureAwait()
    {
        // Arrange & Act
        var action = new OptimizationAction();
        action.Modifications.Add("Added ConfigureAwait(false)");

        // Assert
        Assert.Contains(action.Modifications, m => m.Contains("ConfigureAwait"));
    }

    #endregion

    #region Collection Operations Tests

    [Fact]
    public void OptimizationAction_ModificationsList_SupportsLinq()
    {
        // Arrange
        var action = new OptimizationAction();
        action.Modifications.Add("Task -> ValueTask");
        action.Modifications.Add("Added [Handle]");
        action.Modifications.Add("Handle -> HandleAsync");

        // Act
        var taskRelated = action.Modifications.Where(m => m.Contains("Task")).ToList();

        // Assert
        Assert.Single(taskRelated);
    }

    [Fact]
    public void OptimizationAction_ModificationsList_CanBeIterated()
    {
        // Arrange
        var action = new OptimizationAction();
        action.Modifications.Add("Mod1");
        action.Modifications.Add("Mod2");
        action.Modifications.Add("Mod3");

        // Act
        var count = 0;
        foreach (var mod in action.Modifications)
        {
            count++;
        }

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void OptimizationAction_ModificationsList_SupportsIndexing()
    {
        // Arrange
        var action = new OptimizationAction();
        action.Modifications.Add("First");
        action.Modifications.Add("Second");

        // Act
        var first = action.Modifications[0];
        var second = action.Modifications[1];

        // Assert
        Assert.Equal("First", first);
        Assert.Equal("Second", second);
    }

    #endregion

    #region Multiple Actions Tests

    [Fact]
    public void OptimizationAction_MultipleInstances_ShouldBeIndependent()
    {
        // Arrange & Act
        var action1 = new OptimizationAction { Type = "Handler" };
        var action2 = new OptimizationAction { Type = "Request" };

        action1.Modifications.Add("Mod1");
        action2.Modifications.Add("Mod2");

        // Assert
        Assert.NotEqual(action2.Type, action1.Type);
        Assert.DoesNotContain("Mod2", action1.Modifications);
        Assert.DoesNotContain("Mod1", action2.Modifications);
    }

    [Fact]
    public void OptimizationAction_Collection_CanGroupByType()
    {
        // Arrange
        var actions = new[]
        {
            new OptimizationAction { Type = "Handler", FilePath = "Handler1.cs" },
            new OptimizationAction { Type = "Handler", FilePath = "Handler2.cs" },
            new OptimizationAction { Type = "Request", FilePath = "Request1.cs" }
        };

        // Act
        var grouped = actions.GroupBy(a => a.Type).ToList();

        // Assert
        Assert.Equal(2, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == "Handler").Count());
    }

    [Fact]
    public void OptimizationAction_Collection_CanFilterByType()
    {
        // Arrange
        var actions = new[]
        {
            new OptimizationAction { Type = "Handler" },
            new OptimizationAction { Type = "Request" },
            new OptimizationAction { Type = "Handler" }
        };

        // Act
        var handlers = actions.Where(a => a.Type == "Handler").ToList();

        // Assert
        Assert.Equal(2, handlers.Count());
    }

    [Fact]
    public void OptimizationAction_Collection_CanCountModifications()
    {
        // Arrange
        var actions = new[]
        {
            new OptimizationAction { Modifications = new List<string> { "Mod1", "Mod2" } },
            new OptimizationAction { Modifications = new List<string> { "Mod3" } }
        };

        // Act
        var totalMods = actions.Sum(a => a.Modifications.Count);

        // Assert
        Assert.Equal(3, totalMods);
    }

    #endregion

    #region Real-World Scenarios Tests

    [Fact]
    public void OptimizationAction_HandlerOptimization_CompleteScenario()
    {
        // Arrange & Act
        var action = new OptimizationAction
        {
            FilePath = @"C:\Project\Handlers\GetUserHandler.cs",
            Type = "Handler",
            OriginalContent = @"public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken ct)
    {
        return await _repository.GetUserAsync(request.UserId);
    }
}",
            OptimizedContent = @"public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    [Handle]
    public async ValueTask<User> HandleAsync(GetUserQuery request, CancellationToken ct)
    {
        return await _repository.GetUserAsync(request.UserId).ConfigureAwait(false);
    }
}",
            Modifications = new List<string>
            {
                "Replaced Task<User> with ValueTask<User>",
                "Added [Handle] attribute",
                "Renamed Handle to HandleAsync",
                "Added ConfigureAwait(false)"
            }
        };

        // Assert
        Assert.EndsWith("GetUserHandler.cs", action.FilePath);
        Assert.Equal("Handler", action.Type);
        Assert.Equal(4, action.Modifications.Count());
        Assert.Contains("ValueTask", action.OptimizedContent);
        Assert.Contains("[Handle]", action.OptimizedContent);
    }

    [Fact]
    public void OptimizationAction_ConfigOptimization_CompleteScenario()
    {
        // Arrange & Act
        var action = new OptimizationAction
        {
            FilePath = @"C:\Project\appsettings.json",
            Type = "Config",
            OriginalContent = @"{
  ""Relay"": {
    ""EnableCaching"": false
  }
}",
            OptimizedContent = @"{
  ""Relay"": {
    ""EnableCaching"": true,
    ""CacheExpirationMinutes"": 30
  }
}",
            Modifications = new List<string>
            {
                "Enabled caching",
                "Added cache expiration setting"
            }
        };

        // Assert
        Assert.Equal("Config", action.Type);
        Assert.Equal(2, action.Modifications.Count());
        Assert.Contains("\"EnableCaching\": true", action.OptimizedContent);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void OptimizationAction_EmptyFilePath_ShouldBeAllowed()
    {
        // Arrange & Act
        var action = new OptimizationAction { FilePath = "" };

        // Assert
        Assert.Equal("", action.FilePath);
    }

    [Fact]
    public void OptimizationAction_EmptyType_ShouldBeAllowed()
    {
        // Arrange & Act
        var action = new OptimizationAction { Type = "" };

        // Assert
        Assert.Equal("", action.Type);
    }

    [Fact]
    public void OptimizationAction_NullModificationsList_CanBeReplaced()
    {
        // Arrange
        var action = new OptimizationAction
        {
            Modifications = null!
        };

        // Act
        action.Modifications = new List<string>();

        // Assert
        Assert.NotNull(action.Modifications);
        Assert.Empty(action.Modifications);
    }

    [Fact]
    public void OptimizationAction_VeryLongFilePath_ShouldBeHandled()
    {
        // Arrange
        var longPath = string.Join("\\", Enumerable.Range(1, 50).Select(i => $"Folder{i}")) + "\\File.cs";

        // Act
        var action = new OptimizationAction { FilePath = longPath };

        // Assert
        Assert.Contains("File.cs", action.FilePath);
        Assert.True(action.FilePath.Length > 100);
    }

    [Fact]
    public void OptimizationAction_VeryLongContent_ShouldBeHandled()
    {
        // Arrange
        var longContent = new string('A', 10000);

        // Act
        var action = new OptimizationAction
        {
            OriginalContent = longContent,
            OptimizedContent = longContent
        };

        // Assert
        Assert.Equal(10000, action.OriginalContent.Length);
        Assert.Equal(10000, action.OptimizedContent.Length);
    }

    [Fact]
    public void OptimizationAction_ManyModifications_ShouldBeHandled()
    {
        // Arrange
        var action = new OptimizationAction();

        // Act
        for (int i = 0; i < 100; i++)
        {
            action.Modifications.Add($"Modification {i}");
        }

        // Assert
        Assert.Equal(100, action.Modifications.Count());
    }

    #endregion

    #region Property Validation Tests

    [Fact]
    public void OptimizationAction_Properties_ShouldHavePublicGetters()
    {
        // Arrange
        var type = typeof(OptimizationAction);

        // Act
        var filePathProperty = type.GetProperty(nameof(OptimizationAction.FilePath));
        var typeProperty = type.GetProperty(nameof(OptimizationAction.Type));
        var modsProperty = type.GetProperty(nameof(OptimizationAction.Modifications));

        // Assert
        Assert.NotNull(filePathProperty);
        Assert.NotNull(typeProperty);
        Assert.NotNull(modsProperty);
        Assert.True(filePathProperty!.CanRead);
        Assert.True(typeProperty!.CanRead);
        Assert.True(modsProperty!.CanRead);
    }

    [Fact]
    public void OptimizationAction_Properties_ShouldHavePublicSetters()
    {
        // Arrange
        var type = typeof(OptimizationAction);

        // Act
        var filePathProperty = type.GetProperty(nameof(OptimizationAction.FilePath));
        var typeProperty = type.GetProperty(nameof(OptimizationAction.Type));

        // Assert
        Assert.True(filePathProperty!.CanWrite);
        Assert.True(typeProperty!.CanWrite);
    }

    #endregion

    #region Summary Statistics Tests

    [Fact]
    public void OptimizationAction_Statistics_CanCalculateModificationCount()
    {
        // Arrange
        var action = new OptimizationAction();
        action.Modifications.Add("Mod1");
        action.Modifications.Add("Mod2");
        action.Modifications.Add("Mod3");

        // Act
        var count = action.Modifications.Count;

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void OptimizationAction_Statistics_CanMeasureContentChange()
    {
        // Arrange
        var action = new OptimizationAction
        {
            OriginalContent = "short",
            OptimizedContent = "much longer content here"
        };

        // Act
        var originalLength = action.OriginalContent.Length;
        var optimizedLength = action.OptimizedContent.Length;
        var diff = optimizedLength - originalLength;

        // Assert
        Assert.True(diff > 0);
    }

    #endregion

    #region String Manipulation Tests

    [Fact]
    public void OptimizationAction_FilePath_CanNormalize()
    {
        // Arrange
        var action = new OptimizationAction
        {
            FilePath = @"C:\Projects\Relay\src\Handlers\Handler.cs"
        };

        // Act
        var normalized = action.FilePath.Replace('\\', '/');

        // Assert
        Assert.Contains("/", normalized);
    }

    [Fact]
    public void OptimizationAction_Type_CanConvertCase()
    {
        // Arrange
        var action = new OptimizationAction { Type = "Handler" };

        // Act
        var lower = action.Type.ToLowerInvariant();
        var upper = action.Type.ToUpperInvariant();

        // Assert
        Assert.Equal("handler", lower);
        Assert.Equal("HANDLER", upper);
    }

    #endregion
}


