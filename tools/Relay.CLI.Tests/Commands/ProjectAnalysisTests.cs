using Relay.CLI.Commands.Models;
using Relay.CLI.Commands.Models.Performance;

namespace Relay.CLI.Tests.Commands;

public class ProjectAnalysisTests
{
    [Fact]
    public void ProjectAnalysis_ShouldHaveProjectPathProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { ProjectPath = "/path/to/project" };

        // Assert
        analysis.ProjectPath.Should().Be("/path/to/project");
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveAnalysisDepthProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { AnalysisDepth = "Full" };

        // Assert
        analysis.AnalysisDepth.Should().Be("Full");
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveIncludeTestsProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { IncludeTests = true };

        // Assert
        analysis.IncludeTests.Should().BeTrue();
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveTimestampProperty()
    {
        // Arrange
        var timestamp = new DateTime(2023, 10, 15, 14, 30, 0);

        // Act
        var analysis = new ProjectAnalysis { Timestamp = timestamp };

        // Assert
        analysis.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveProjectFilesProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();
        var files = new List<string> { "file1.cs", "file2.cs" };

        // Act
        analysis.ProjectFiles = files;

        // Assert
        analysis.ProjectFiles.Should().BeEquivalentTo(files);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveSourceFilesProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();
        var files = new List<string> { "src/file1.cs", "src/file2.cs" };

        // Act
        analysis.SourceFiles = files;

        // Assert
        analysis.SourceFiles.Should().BeEquivalentTo(files);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHandlersProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();
        var handlers = new List<HandlerInfo> { new HandlerInfo { Name = "Handler1" } };

        // Act
        analysis.Handlers = handlers;

        // Assert
        analysis.Handlers.Should().BeEquivalentTo(handlers);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveRequestsProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();
        var requests = new List<RequestInfo> { new RequestInfo { Name = "Request1" } };

        // Act
        analysis.Requests = requests;

        // Assert
        analysis.Requests.Should().BeEquivalentTo(requests);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHavePerformanceIssuesProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();
        var issues = new List<PerformanceIssue> { new PerformanceIssue { Type = "Memory" } };

        // Act
        analysis.PerformanceIssues = issues;

        // Assert
        analysis.PerformanceIssues.Should().BeEquivalentTo(issues);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveReliabilityIssuesProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();
        var issues = new List<ReliabilityIssue> { new ReliabilityIssue { Type = "Timeout" } };

        // Act
        analysis.ReliabilityIssues = issues;

        // Assert
        analysis.ReliabilityIssues.Should().BeEquivalentTo(issues);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveRecommendationsProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();
        var recommendations = new List<Recommendation> { new Recommendation { Title = "Optimize DB" } };

        // Act
        analysis.Recommendations = recommendations;

        // Assert
        analysis.Recommendations.Should().BeEquivalentTo(recommendations);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasRelayCoreProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasRelayCore = true };

        // Assert
        analysis.HasRelayCore.Should().BeTrue();
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasMediatRProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasMediatR = true };

        // Assert
        analysis.HasMediatR.Should().BeTrue();
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasLoggingProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasLogging = true };

        // Assert
        analysis.HasLogging.Should().BeTrue();
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasValidationProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasValidation = true };

        // Assert
        analysis.HasValidation.Should().BeTrue();
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasCachingProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasCaching = true };

        // Assert
        analysis.HasCaching.Should().BeTrue();
    }

    [Fact]
    public void ProjectAnalysis_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();

        // Assert
        analysis.ProjectPath.Should().Be("");
        analysis.AnalysisDepth.Should().Be("");
        analysis.IncludeTests.Should().BeFalse();
        analysis.Timestamp.Should().Be(DateTime.MinValue);
        analysis.ProjectFiles.Should().NotBeNull();
        analysis.ProjectFiles.Should().BeEmpty();
        analysis.SourceFiles.Should().NotBeNull();
        analysis.SourceFiles.Should().BeEmpty();
        analysis.Handlers.Should().NotBeNull();
        analysis.Handlers.Should().BeEmpty();
        analysis.Requests.Should().NotBeNull();
        analysis.Requests.Should().BeEmpty();
        analysis.PerformanceIssues.Should().NotBeNull();
        analysis.PerformanceIssues.Should().BeEmpty();
        analysis.ReliabilityIssues.Should().NotBeNull();
        analysis.ReliabilityIssues.Should().BeEmpty();
        analysis.Recommendations.Should().NotBeNull();
        analysis.Recommendations.Should().BeEmpty();
        analysis.HasRelayCore.Should().BeFalse();
        analysis.HasMediatR.Should().BeFalse();
        analysis.HasLogging.Should().BeFalse();
        analysis.HasValidation.Should().BeFalse();
        analysis.HasCaching.Should().BeFalse();
    }

    [Fact]
    public void ProjectAnalysis_CanSetAllPropertiesViaInitializer()
    {
        // Arrange
        var timestamp = DateTime.Now;

        // Act
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "/home/user/myproject",
            AnalysisDepth = "Comprehensive",
            IncludeTests = true,
            Timestamp = timestamp,
            ProjectFiles = new List<string> { "MyProject.csproj", "README.md" },
            SourceFiles = new List<string> { "Program.cs", "Startup.cs" },
            Handlers = new List<HandlerInfo> { new HandlerInfo { Name = "CreateUserHandler" } },
            Requests = new List<RequestInfo> { new RequestInfo { Name = "CreateUserRequest" } },
            PerformanceIssues = new List<PerformanceIssue> { new PerformanceIssue { Type = "Memory Leak" } },
            ReliabilityIssues = new List<ReliabilityIssue> { new ReliabilityIssue { Type = "Timeout" } },
            Recommendations = new List<Recommendation> { new Recommendation { Title = "Add Caching" } },
            HasRelayCore = true,
            HasMediatR = true,
            HasLogging = true,
            HasValidation = true,
            HasCaching = false
        };

        // Assert
        analysis.ProjectPath.Should().Be("/home/user/myproject");
        analysis.AnalysisDepth.Should().Be("Comprehensive");
        analysis.IncludeTests.Should().BeTrue();
        analysis.Timestamp.Should().Be(timestamp);
        analysis.ProjectFiles.Should().HaveCount(2);
        analysis.SourceFiles.Should().HaveCount(2);
        analysis.Handlers.Should().HaveCount(1);
        analysis.Requests.Should().HaveCount(1);
        analysis.PerformanceIssues.Should().HaveCount(1);
        analysis.ReliabilityIssues.Should().HaveCount(1);
        analysis.Recommendations.Should().HaveCount(1);
        analysis.HasRelayCore.Should().BeTrue();
        analysis.HasMediatR.Should().BeTrue();
        analysis.HasLogging.Should().BeTrue();
        analysis.HasValidation.Should().BeTrue();
        analysis.HasCaching.Should().BeFalse();
    }

    [Fact]
    public void ProjectAnalysis_CanAddProjectFiles()
    {
        // Arrange
        var analysis = new ProjectAnalysis();

        // Act
        analysis.ProjectFiles.Add("MyProject.csproj");
        analysis.ProjectFiles.Add("appsettings.json");

        // Assert
        analysis.ProjectFiles.Should().HaveCount(2);
        analysis.ProjectFiles.Should().Contain("MyProject.csproj");
        analysis.ProjectFiles.Should().Contain("appsettings.json");
    }

    [Fact]
    public void ProjectAnalysis_CanAddSourceFiles()
    {
        // Arrange
        var analysis = new ProjectAnalysis();

        // Act
        analysis.SourceFiles.Add("Program.cs");
        analysis.SourceFiles.Add("Controllers/HomeController.cs");

        // Assert
        analysis.SourceFiles.Should().HaveCount(2);
    }

    [Fact]
    public void ProjectAnalysis_CanAddHandlers()
    {
        // Arrange
        var analysis = new ProjectAnalysis();

        // Act
        analysis.Handlers.Add(new HandlerInfo { Name = "CreateUserHandler", IsAsync = true });
        analysis.Handlers.Add(new HandlerInfo { Name = "UpdateUserHandler", IsAsync = false });

        // Assert
        analysis.Handlers.Should().HaveCount(2);
        analysis.Handlers.Count(h => h.IsAsync).Should().Be(1);
    }

    [Fact]
    public void ProjectAnalysis_CanAddPerformanceIssues()
    {
        // Arrange
        var analysis = new ProjectAnalysis();

        // Act
        analysis.PerformanceIssues.Add(new PerformanceIssue { Type = "Memory Leak", Severity = "High" });
        analysis.PerformanceIssues.Add(new PerformanceIssue { Type = "CPU Bottleneck", Severity = "Medium" });

        // Assert
        analysis.PerformanceIssues.Should().HaveCount(2);
        analysis.PerformanceIssues.Count(i => i.Severity == "High").Should().Be(1);
    }

    [Fact]
    public void ProjectAnalysis_CanAddRecommendations()
    {
        // Arrange
        var analysis = new ProjectAnalysis();

        // Act
        analysis.Recommendations.Add(new Recommendation { Title = "Add Caching", Priority = "High" });
        analysis.Recommendations.Add(new Recommendation { Title = "Optimize Queries", Priority = "Medium" });

        // Assert
        analysis.Recommendations.Should().HaveCount(2);
    }

    [Fact]
    public void ProjectAnalysis_BooleanProperties_CanBeTrueOrFalse()
    {
        // Test all boolean properties can be set to true
        var analysisTrue = new ProjectAnalysis
        {
            IncludeTests = true,
            HasRelayCore = true,
            HasMediatR = true,
            HasLogging = true,
            HasValidation = true,
            HasCaching = true
        };

        analysisTrue.IncludeTests.Should().BeTrue();
        analysisTrue.HasRelayCore.Should().BeTrue();
        analysisTrue.HasMediatR.Should().BeTrue();
        analysisTrue.HasLogging.Should().BeTrue();
        analysisTrue.HasValidation.Should().BeTrue();
        analysisTrue.HasCaching.Should().BeTrue();

        // Test all boolean properties can be set to false
        var analysisFalse = new ProjectAnalysis
        {
            IncludeTests = false,
            HasRelayCore = false,
            HasMediatR = false,
            HasLogging = false,
            HasValidation = false,
            HasCaching = false
        };

        analysisFalse.IncludeTests.Should().BeFalse();
        analysisFalse.HasRelayCore.Should().BeFalse();
        analysisFalse.HasMediatR.Should().BeFalse();
        analysisFalse.HasLogging.Should().BeFalse();
        analysisFalse.HasValidation.Should().BeFalse();
        analysisFalse.HasCaching.Should().BeFalse();
    }

    [Fact]
    public void ProjectAnalysis_CanBeUsedInCollections()
    {
        // Arrange & Act
        var analyses = new List<ProjectAnalysis>
        {
            new ProjectAnalysis { ProjectPath = "Project1", HasRelayCore = true },
            new ProjectAnalysis { ProjectPath = "Project2", HasRelayCore = false },
            new ProjectAnalysis { ProjectPath = "Project3", HasRelayCore = true }
        };

        // Assert
        analyses.Should().HaveCount(3);
        analyses.Count(a => a.HasRelayCore).Should().Be(2);
    }

    [Fact]
    public void ProjectAnalysis_CanBeFilteredByFeatures()
    {
        // Arrange
        var analyses = new List<ProjectAnalysis>
        {
            new ProjectAnalysis { ProjectPath = "P1", HasRelayCore = true, HasLogging = true, HasValidation = true },
            new ProjectAnalysis { ProjectPath = "P2", HasRelayCore = false, HasLogging = true, HasValidation = false },
            new ProjectAnalysis { ProjectPath = "P3", HasRelayCore = true, HasLogging = false, HasValidation = true }
        };

        // Act
        var relayProjects = analyses.Where(a => a.HasRelayCore).ToList();
        var projectsWithLogging = analyses.Where(a => a.HasLogging).ToList();
        var projectsWithAllFeatures = analyses.Where(a => a.HasRelayCore && a.HasLogging && a.HasValidation).ToList();

        // Assert
        relayProjects.Should().HaveCount(2);
        projectsWithLogging.Should().HaveCount(2);
        projectsWithAllFeatures.Should().HaveCount(1);
    }

    [Fact]
    public void ProjectAnalysis_CanCalculateAggregates()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Handlers = new List<HandlerInfo>
            {
                new HandlerInfo { Name = "H1", LineCount = 100, IsAsync = true },
                new HandlerInfo { Name = "H2", LineCount = 80, IsAsync = false },
                new HandlerInfo { Name = "H3", LineCount = 120, IsAsync = true }
            },
            PerformanceIssues = new List<PerformanceIssue>
            {
                new PerformanceIssue { Type = "Memory", Count = 5 },
                new PerformanceIssue { Type = "CPU", Count = 3 }
            },
            Recommendations = new List<Recommendation>
            {
                new Recommendation { Title = "R1", Priority = "High" },
                new Recommendation { Title = "R2", Priority = "Medium" }
            }
        };

        // Act
        var totalHandlerLines = analysis.Handlers.Sum(h => h.LineCount);
        var asyncHandlers = analysis.Handlers.Count(h => h.IsAsync);
        var totalIssues = analysis.PerformanceIssues.Sum(i => i.Count);
        var highPriorityRecommendations = analysis.Recommendations.Count(r => r.Priority == "High");

        // Assert
        totalHandlerLines.Should().Be(300);
        asyncHandlers.Should().Be(2);
        totalIssues.Should().Be(8);
        highPriorityRecommendations.Should().Be(1);
    }

    [Fact]
    public void ProjectAnalysis_ShouldBeClass()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();

        // Assert
        analysis.Should().NotBeNull();
        analysis.GetType().IsClass.Should().BeTrue();
    }

    [Fact]
    public void ProjectAnalysis_WithRealisticData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "C:\\Projects\\MyRelayApp",
            AnalysisDepth = "Full",
            IncludeTests = true,
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 45),
            ProjectFiles = new List<string> { "MyRelayApp.csproj", "appsettings.json", "README.md" },
            SourceFiles = new List<string> { "Program.cs", "Startup.cs", "Controllers/UserController.cs" },
            Handlers = new List<HandlerInfo>
            {
                new HandlerInfo { Name = "CreateUserHandler", IsAsync = true, HasLogging = true, LineCount = 45 },
                new HandlerInfo { Name = "GetUserHandler", IsAsync = true, HasLogging = false, LineCount = 25 }
            },
            PerformanceIssues = new List<PerformanceIssue>
            {
                new PerformanceIssue { Type = "N+1 Query", Severity = "High", Count = 3 }
            },
            Recommendations = new List<Recommendation>
            {
                new Recommendation { Title = "Add Response Caching", Priority = "Medium" }
            },
            HasRelayCore = true,
            HasMediatR = true,
            HasLogging = true,
            HasValidation = true,
            HasCaching = false
        };

        // Assert
        analysis.ProjectPath.Should().Be("C:\\Projects\\MyRelayApp");
        analysis.AnalysisDepth.Should().Be("Full");
        analysis.IncludeTests.Should().BeTrue();
        analysis.ProjectFiles.Should().HaveCount(3);
        analysis.SourceFiles.Should().HaveCount(3);
        analysis.Handlers.Should().HaveCount(2);
        analysis.PerformanceIssues.Should().HaveCount(1);
        analysis.Recommendations.Should().HaveCount(1);
        analysis.HasRelayCore.Should().BeTrue();
        analysis.HasMediatR.Should().BeTrue();
        analysis.HasLogging.Should().BeTrue();
        analysis.HasValidation.Should().BeTrue();
        analysis.HasCaching.Should().BeFalse();
    }

    [Fact]
    public void ProjectAnalysis_PropertiesCanBeModified()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "InitialPath",
            AnalysisDepth = "Basic",
            IncludeTests = false
        };

        // Act
        analysis.ProjectPath = "ModifiedPath";
        analysis.AnalysisDepth = "Comprehensive";
        analysis.IncludeTests = true;

        // Assert
        analysis.ProjectPath.Should().Be("ModifiedPath");
        analysis.AnalysisDepth.Should().Be("Comprehensive");
        analysis.IncludeTests.Should().BeTrue();
    }

    [Fact]
    public void ProjectAnalysis_CanBeFilteredByTimestamp()
    {
        // Arrange
        var analyses = new List<ProjectAnalysis>
        {
            new ProjectAnalysis { ProjectPath = "P1", Timestamp = new DateTime(2023, 1, 1) },
            new ProjectAnalysis { ProjectPath = "P2", Timestamp = new DateTime(2023, 6, 1) },
            new ProjectAnalysis { ProjectPath = "P3", Timestamp = new DateTime(2023, 12, 1) }
        };

        // Act
        var recentAnalyses = analyses.Where(a => a.Timestamp > new DateTime(2023, 5, 1)).ToList();

        // Assert
        recentAnalyses.Should().HaveCount(2);
        recentAnalyses.All(a => a.Timestamp > new DateTime(2023, 5, 1)).Should().BeTrue();
    }

    [Fact]
    public void ProjectAnalysis_CanBeGroupedByAnalysisDepth()
    {
        // Arrange
        var analyses = new List<ProjectAnalysis>
        {
            new ProjectAnalysis { ProjectPath = "P1", AnalysisDepth = "Basic", Handlers = new List<HandlerInfo> { new HandlerInfo() } },
            new ProjectAnalysis { ProjectPath = "P2", AnalysisDepth = "Full", Handlers = new List<HandlerInfo> { new HandlerInfo(), new HandlerInfo() } },
            new ProjectAnalysis { ProjectPath = "P3", AnalysisDepth = "Basic", Handlers = new List<HandlerInfo> { new HandlerInfo() } }
        };

        // Act
        var grouped = analyses.GroupBy(a => a.AnalysisDepth);

        // Assert
        grouped.Should().HaveCount(2);
        grouped.First(g => g.Key == "Basic").Should().HaveCount(2);
        grouped.First(g => g.Key == "Full").Should().HaveCount(1);
    }

    [Fact]
    public void ProjectAnalysis_CanCalculateHealthScore()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            HasRelayCore = true,
            HasMediatR = true,
            HasLogging = true,
            HasValidation = true,
            HasCaching = false,
            PerformanceIssues = new List<PerformanceIssue>
            {
                new PerformanceIssue { Severity = "High", Count = 2 },
                new PerformanceIssue { Severity = "Medium", Count = 3 },
                new PerformanceIssue { Severity = "Low", Count = 5 }
            },
            ReliabilityIssues = new List<ReliabilityIssue>
            {
                new ReliabilityIssue { Severity = "High", Count = 1 }
            }
        };

        // Act - Simulate health score calculation
        var featureScore = (analysis.HasRelayCore ? 20 : 0) +
                          (analysis.HasMediatR ? 20 : 0) +
                          (analysis.HasLogging ? 15 : 0) +
                          (analysis.HasValidation ? 15 : 0) +
                          (analysis.HasCaching ? 10 : 0);

        var issuePenalty = analysis.PerformanceIssues.Sum(i => i.Count) +
                          analysis.ReliabilityIssues.Sum(i => i.Count) * 2;

        var healthScore = Math.Max(0, featureScore - issuePenalty);

        // Assert
        featureScore.Should().Be(70); // 20 + 20 + 15 + 15 + 0
        issuePenalty.Should().Be(12); // 10 + 1*2
        healthScore.Should().Be(58);
    }

    [Fact]
    public void ProjectAnalysis_CanGenerateSummaryReport()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "/path/to/project",
            AnalysisDepth = "Full",
            IncludeTests = true,
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 0),
            ProjectFiles = new List<string> { "Project.csproj", "README.md", "LICENSE" },
            SourceFiles = new List<string> { "Program.cs", "Startup.cs", "Controllers/HomeController.cs" },
            Handlers = new List<HandlerInfo>
            {
                new HandlerInfo { Name = "CreateUserHandler", IsAsync = true },
                new HandlerInfo { Name = "GetUserHandler", IsAsync = true },
                new HandlerInfo { Name = "UpdateUserHandler", IsAsync = false }
            },
            PerformanceIssues = new List<PerformanceIssue>
            {
                new PerformanceIssue { Type = "Memory Leak", Severity = "High" },
                new PerformanceIssue { Type = "Slow Query", Severity = "Medium" }
            },
            Recommendations = new List<Recommendation>
            {
                new Recommendation { Title = "Add Caching", Priority = "High" },
                new Recommendation { Title = "Optimize DB Queries", Priority = "Medium" }
            },
            HasRelayCore = true,
            HasMediatR = true,
            HasLogging = true,
            HasValidation = true,
            HasCaching = false
        };

        // Act - Simulate summary report generation
        var summary = new
        {
            Project = analysis.ProjectPath,
            FilesAnalyzed = analysis.SourceFiles.Count,
            HandlersFound = analysis.Handlers.Count,
            AsyncHandlers = analysis.Handlers.Count(h => h.IsAsync),
            IssuesFound = analysis.PerformanceIssues.Count,
            RecommendationsCount = analysis.Recommendations.Count,
            FeaturesEnabled = new[] { analysis.HasRelayCore, analysis.HasMediatR, analysis.HasLogging, analysis.HasValidation, analysis.HasCaching }.Count(f => f)
        };

        // Assert
        summary.Project.Should().Be("/path/to/project");
        summary.FilesAnalyzed.Should().Be(3);
        summary.HandlersFound.Should().Be(3);
        summary.AsyncHandlers.Should().Be(2);
        summary.IssuesFound.Should().Be(2);
        summary.RecommendationsCount.Should().Be(2);
        summary.FeaturesEnabled.Should().Be(4);
    }
}