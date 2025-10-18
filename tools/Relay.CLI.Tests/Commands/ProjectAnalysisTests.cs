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
        Assert.Equal("/path/to/project", analysis.ProjectPath);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveAnalysisDepthProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { AnalysisDepth = "Full" };

        // Assert
        Assert.Equal("Full", analysis.AnalysisDepth);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveIncludeTestsProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { IncludeTests = true };

        // Assert
        Assert.True(analysis.IncludeTests);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveTimestampProperty()
    {
        // Arrange
        var timestamp = new DateTime(2023, 10, 15, 14, 30, 0);

        // Act
        var analysis = new ProjectAnalysis { Timestamp = timestamp };

        // Assert
        Assert.Equal(timestamp, analysis.Timestamp);
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
        Assert.Equal(files, analysis.ProjectFiles);
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
        Assert.Equal(files, analysis.SourceFiles);
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
        Assert.Equal(handlers, analysis.Handlers);
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
        Assert.Equal(requests, analysis.Requests);
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
        Assert.Equal(issues, analysis.PerformanceIssues);
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
        Assert.Equal(issues, analysis.ReliabilityIssues);
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
        Assert.Equal(recommendations, analysis.Recommendations);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasRelayCoreProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasRelayCore = true };

        // Assert
        Assert.True(analysis.HasRelayCore);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasMediatRProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasMediatR = true };

        // Assert
        Assert.True(analysis.HasMediatR);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasLoggingProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasLogging = true };

        // Assert
        Assert.True(analysis.HasLogging);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasValidationProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasValidation = true };

        // Assert
        Assert.True(analysis.HasValidation);
    }

    [Fact]
    public void ProjectAnalysis_ShouldHaveHasCachingProperty()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis { HasCaching = true };

        // Assert
        Assert.True(analysis.HasCaching);
    }

    [Fact]
    public void ProjectAnalysis_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();

        // Assert
        Assert.Equal("", analysis.ProjectPath);
        Assert.Equal("", analysis.AnalysisDepth);
        Assert.False(analysis.IncludeTests);
        Assert.Equal(DateTime.MinValue, analysis.Timestamp);
        Assert.NotNull(analysis.ProjectFiles);
        Assert.Empty(analysis.ProjectFiles);
        Assert.NotNull(analysis.SourceFiles);
        Assert.Empty(analysis.SourceFiles);
        Assert.NotNull(analysis.Handlers);
        Assert.Empty(analysis.Handlers);
        Assert.NotNull(analysis.Requests);
        Assert.Empty(analysis.Requests);
        Assert.NotNull(analysis.PerformanceIssues);
        Assert.Empty(analysis.PerformanceIssues);
        Assert.NotNull(analysis.ReliabilityIssues);
        Assert.Empty(analysis.ReliabilityIssues);
        Assert.NotNull(analysis.Recommendations);
        Assert.Empty(analysis.Recommendations);
        Assert.False(analysis.HasRelayCore);
        Assert.False(analysis.HasMediatR);
        Assert.False(analysis.HasLogging);
        Assert.False(analysis.HasValidation);
        Assert.False(analysis.HasCaching);
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
        Assert.Equal("/home/user/myproject", analysis.ProjectPath);
        Assert.Equal("Comprehensive", analysis.AnalysisDepth);
        Assert.True(analysis.IncludeTests);
        Assert.Equal(timestamp, analysis.Timestamp);
        Assert.Equal(2, analysis.ProjectFiles.Count());
        Assert.Equal(2, analysis.SourceFiles.Count());
        Assert.Equal(1, analysis.Handlers.Count());
        Assert.Equal(1, analysis.Requests.Count());
        Assert.Equal(1, analysis.PerformanceIssues.Count());
        Assert.Equal(1, analysis.ReliabilityIssues.Count());
        Assert.Equal(1, analysis.Recommendations.Count());
        Assert.True(analysis.HasRelayCore);
        Assert.True(analysis.HasMediatR);
        Assert.True(analysis.HasLogging);
        Assert.True(analysis.HasValidation);
        Assert.False(analysis.HasCaching);
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
        Assert.Equal(2, analysis.ProjectFiles.Count());
        Assert.Contains("MyProject.csproj", analysis.ProjectFiles);
        Assert.Contains("appsettings.json", analysis.ProjectFiles);
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
        Assert.Equal(2, analysis.SourceFiles.Count());
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
        Assert.Equal(2, analysis.Handlers.Count());
        Assert.Equal(1, analysis.Handlers.Count(h => h.IsAsync));
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
        Assert.Equal(2, analysis.PerformanceIssues.Count());
        Assert.Equal(1, analysis.PerformanceIssues.Count(i => i.Severity == "High"));
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
        Assert.Equal(2, analysis.Recommendations.Count());
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

        Assert.True(analysisTrue.IncludeTests);
        Assert.True(analysisTrue.HasRelayCore);
        Assert.True(analysisTrue.HasMediatR);
        Assert.True(analysisTrue.HasLogging);
        Assert.True(analysisTrue.HasValidation);
        Assert.True(analysisTrue.HasCaching);

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

        Assert.False(analysisFalse.IncludeTests);
        Assert.False(analysisFalse.HasRelayCore);
        Assert.False(analysisFalse.HasMediatR);
        Assert.False(analysisFalse.HasLogging);
        Assert.False(analysisFalse.HasValidation);
        Assert.False(analysisFalse.HasCaching);
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
        Assert.Equal(3, analyses.Count());
        Assert.Equal(2, analyses.Count(a => a.HasRelayCore));
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
        Assert.Equal(2, relayProjects.Count());
        Assert.Equal(2, projectsWithLogging.Count());
        Assert.Equal(1, projectsWithAllFeatures.Count());
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
        Assert.Equal(300, totalHandlerLines);
        Assert.Equal(2, asyncHandlers);
        Assert.Equal(8, totalIssues);
        Assert.Equal(1, highPriorityRecommendations);
    }

    [Fact]
    public void ProjectAnalysis_ShouldBeClass()
    {
        // Arrange & Act
        var analysis = new ProjectAnalysis();

        // Assert
        Assert.NotNull(analysis);
        Assert.True(analysis.GetType().IsClass);
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
        Assert.Equal("C:\\Projects\\MyRelayApp", analysis.ProjectPath);
        Assert.Equal("Full", analysis.AnalysisDepth);
        Assert.True(analysis.IncludeTests);
        Assert.Equal(3, analysis.ProjectFiles.Count());
        Assert.Equal(3, analysis.SourceFiles.Count());
        Assert.Equal(2, analysis.Handlers.Count());
        Assert.Equal(1, analysis.PerformanceIssues.Count());
        Assert.Equal(1, analysis.Recommendations.Count());
        Assert.True(analysis.HasRelayCore);
        Assert.True(analysis.HasMediatR);
        Assert.True(analysis.HasLogging);
        Assert.True(analysis.HasValidation);
        Assert.False(analysis.HasCaching);
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
        Assert.Equal("ModifiedPath", analysis.ProjectPath);
        Assert.Equal("Comprehensive", analysis.AnalysisDepth);
        Assert.True(analysis.IncludeTests);
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
        Assert.Equal(2, recentAnalyses.Count());
        Assert.True(recentAnalyses.All(a => a.Timestamp > new DateTime(2023, 5, 1)));
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
        Assert.Equal(2, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == "Basic").Count());
        Assert.Equal(1, grouped.First(g => g.Key == "Full").Count());
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
        Assert.Equal(70, featureScore); // 20 + 20 + 15 + 15 + 0
        Assert.Equal(12, issuePenalty); // 10 + 1*2
        Assert.Equal(58, healthScore);
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
        Assert.Equal("/path/to/project", summary.Project);
        Assert.Equal(3, summary.FilesAnalyzed);
        Assert.Equal(3, summary.HandlersFound);
        Assert.Equal(2, summary.AsyncHandlers);
        Assert.Equal(2, summary.IssuesFound);
        Assert.Equal(2, summary.RecommendationsCount);
        Assert.Equal(4, summary.FeaturesEnabled);
    }
}

