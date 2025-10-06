using FluentAssertions;
using Relay.CLI.Commands;
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
        action.FilePath.Should().BeEmpty();
        action.Type.Should().BeEmpty();
        action.Modifications.Should().NotBeNull();
        action.Modifications.Should().BeEmpty();
        action.OriginalContent.Should().BeEmpty();
        action.OptimizedContent.Should().BeEmpty();
    }

    [Fact]
    public void OptimizationAction_NewInstance_ShouldHaveEmptyModificationsList()
    {
        // Act
        var action = new OptimizationAction();

        // Assert
        action.Modifications.Should().NotBeNull();
        action.Modifications.Should().HaveCount(0);
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
        action.FilePath.Should().Be(expectedPath);
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
        action.Type.Should().Be(expectedType);
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
        action.Modifications.Should().BeEquivalentTo(modifications);
        action.Modifications.Should().HaveCount(2);
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
        action.OriginalContent.Should().Be(originalContent);
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
        action.OptimizedContent.Should().Be(optimizedContent);
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
        action.FilePath.Should().Be(@"C:\Projects\Handler.cs");
        action.Type.Should().Be("Handler");
        action.Modifications.Should().HaveCount(1);
        action.OriginalContent.Should().NotBeEmpty();
        action.OptimizedContent.Should().NotBeEmpty();
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
        action.Modifications.Should().HaveCount(1);
        action.Modifications.Should().Contain("Task -> ValueTask");
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
        action.Modifications.Should().HaveCount(3);
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
        action.Modifications.Should().BeEmpty();
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
        contains.Should().BeTrue();
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
        action.Modifications.Should().HaveCount(1);
        action.Modifications.Should().NotContain("Task -> ValueTask");
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
        action.Type.Should().Be(type);
    }

    [Fact]
    public void OptimizationAction_Type_Handler_ShouldBeRecognized()
    {
        // Arrange & Act
        var action = new OptimizationAction { Type = "Handler" };

        // Assert
        action.Type.Should().Be("Handler");
    }

    [Fact]
    public void OptimizationAction_Type_Request_ShouldBeRecognized()
    {
        // Arrange & Act
        var action = new OptimizationAction { Type = "Request" };

        // Assert
        action.Type.Should().Be("Request");
    }

    [Fact]
    public void OptimizationAction_Type_Config_ShouldBeRecognized()
    {
        // Arrange & Act
        var action = new OptimizationAction { Type = "Config" };

        // Assert
        action.Type.Should().Be("Config");
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
        action.FilePath.Should().Contain("GetUserHandler.cs");
        Path.IsPathRooted(action.FilePath).Should().BeTrue();
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
        action.FilePath.Should().Contain("GetUserHandler.cs");
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
        action.FilePath.Should().Contain("GetUserHandler.cs");
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
        fileName.Should().Be("Handler.cs");
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
        directory.Should().Contain("Handlers");
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
        isDifferent.Should().BeTrue();
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
        isSame.Should().BeTrue();
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
        action.OriginalContent.Length.Should().BeLessThan(action.OptimizedContent.Length);
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
        action.Modifications.Should().Contain(m => m.Contains("ValueTask"));
    }

    [Fact]
    public void OptimizationAction_Modification_HandleAttribute()
    {
        // Arrange & Act
        var action = new OptimizationAction();
        action.Modifications.Add("Added [Handle] attribute");

        // Assert
        action.Modifications.Should().Contain(m => m.Contains("[Handle]"));
    }

    [Fact]
    public void OptimizationAction_Modification_MethodRename()
    {
        // Arrange & Act
        var action = new OptimizationAction();
        action.Modifications.Add("Renamed Handle() to HandleAsync()");

        // Assert
        action.Modifications.Should().Contain(m => m.Contains("HandleAsync"));
    }

    [Fact]
    public void OptimizationAction_Modification_CancellationToken()
    {
        // Arrange & Act
        var action = new OptimizationAction();
        action.Modifications.Add("Added CancellationToken parameter");

        // Assert
        action.Modifications.Should().Contain(m => m.Contains("CancellationToken"));
    }

    [Fact]
    public void OptimizationAction_Modification_ConfigureAwait()
    {
        // Arrange & Act
        var action = new OptimizationAction();
        action.Modifications.Add("Added ConfigureAwait(false)");

        // Assert
        action.Modifications.Should().Contain(m => m.Contains("ConfigureAwait"));
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
        taskRelated.Should().HaveCount(1);
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
        count.Should().Be(3);
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
        first.Should().Be("First");
        second.Should().Be("Second");
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
        action1.Type.Should().NotBe(action2.Type);
        action1.Modifications.Should().NotContain("Mod2");
        action2.Modifications.Should().NotContain("Mod1");
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
        grouped.Should().HaveCount(2);
        grouped.First(g => g.Key == "Handler").Should().HaveCount(2);
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
        handlers.Should().HaveCount(2);
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
        totalMods.Should().Be(3);
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
        action.FilePath.Should().EndWith("GetUserHandler.cs");
        action.Type.Should().Be("Handler");
        action.Modifications.Should().HaveCount(4);
        action.OptimizedContent.Should().Contain("ValueTask");
        action.OptimizedContent.Should().Contain("[Handle]");
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
        action.Type.Should().Be("Config");
        action.Modifications.Should().HaveCount(2);
        action.OptimizedContent.Should().Contain("\"EnableCaching\": true");
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void OptimizationAction_EmptyFilePath_ShouldBeAllowed()
    {
        // Arrange & Act
        var action = new OptimizationAction { FilePath = "" };

        // Assert
        action.FilePath.Should().BeEmpty();
    }

    [Fact]
    public void OptimizationAction_EmptyType_ShouldBeAllowed()
    {
        // Arrange & Act
        var action = new OptimizationAction { Type = "" };

        // Assert
        action.Type.Should().BeEmpty();
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
        action.Modifications.Should().NotBeNull();
        action.Modifications.Should().BeEmpty();
    }

    [Fact]
    public void OptimizationAction_VeryLongFilePath_ShouldBeHandled()
    {
        // Arrange
        var longPath = string.Join("\\", Enumerable.Range(1, 50).Select(i => $"Folder{i}")) + "\\File.cs";

        // Act
        var action = new OptimizationAction { FilePath = longPath };

        // Assert
        action.FilePath.Should().Contain("File.cs");
        action.FilePath.Length.Should().BeGreaterThan(100);
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
        action.OriginalContent.Length.Should().Be(10000);
        action.OptimizedContent.Length.Should().Be(10000);
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
        action.Modifications.Should().HaveCount(100);
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
        filePathProperty.Should().NotBeNull();
        typeProperty.Should().NotBeNull();
        modsProperty.Should().NotBeNull();
        filePathProperty!.CanRead.Should().BeTrue();
        typeProperty!.CanRead.Should().BeTrue();
        modsProperty!.CanRead.Should().BeTrue();
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
        filePathProperty!.CanWrite.Should().BeTrue();
        typeProperty!.CanWrite.Should().BeTrue();
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
        count.Should().Be(3);
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
        diff.Should().BeGreaterThan(0);
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
        normalized.Should().Contain("/");
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
        lower.Should().Be("handler");
        upper.Should().Be("HANDLER");
    }

    #endregion
}
