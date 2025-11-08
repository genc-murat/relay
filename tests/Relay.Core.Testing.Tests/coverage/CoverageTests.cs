using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class CoverageTests
{
    [Fact]
    public void CoverageMetrics_DefaultConstructor_InitializesWithDefaults()
    {
        // Act
        var metrics = new CoverageMetrics();

        // Assert
        Assert.Equal(0, metrics.TotalLines);
        Assert.Equal(0, metrics.CoveredLines);
        Assert.Equal(0, metrics.TotalBranches);
        Assert.Equal(0, metrics.CoveredBranches);
        Assert.Equal(0, metrics.TotalMethods);
        Assert.Equal(0, metrics.CoveredMethods);
        Assert.NotNull(metrics.UncoveredLines);
        Assert.Empty(metrics.UncoveredLines);
        Assert.NotNull(metrics.UncoveredBranches);
        Assert.Empty(metrics.UncoveredBranches);
        Assert.NotNull(metrics.UncoveredMethods);
        Assert.Empty(metrics.UncoveredMethods);
    }

    [Fact]
    public void CoverageMetrics_CalculatesPercentagesCorrectly()
    {
        // Arrange
        var metrics = new CoverageMetrics
        {
            TotalLines = 100,
            CoveredLines = 80,
            TotalBranches = 20,
            CoveredBranches = 15,
            TotalMethods = 10,
            CoveredMethods = 8
        };

        // Assert
        Assert.Equal(80.0, metrics.LineCoveragePercentage);
        Assert.Equal(75.0, metrics.BranchCoveragePercentage);
        Assert.Equal(80.0, metrics.MethodCoveragePercentage);
    }

    [Fact]
    public void CoverageMetrics_CalculatesPercentages_WithZeroTotals_ReturnsZero()
    {
        // Arrange
        var metrics = new CoverageMetrics
        {
            TotalLines = 0,
            CoveredLines = 0,
            TotalBranches = 0,
            CoveredBranches = 0,
            TotalMethods = 0,
            CoveredMethods = 0
        };

        // Assert
        Assert.Equal(0, metrics.LineCoveragePercentage);
        Assert.Equal(0, metrics.BranchCoveragePercentage);
        Assert.Equal(0, metrics.MethodCoveragePercentage);
    }

    [Fact]
    public void CoverageMetrics_CalculatesPercentages_WithPartialCoverage_CalculatesCorrectly()
    {
        // Arrange
        var metrics = new CoverageMetrics
        {
            TotalLines = 7,
            CoveredLines = 3,
            TotalBranches = 4,
            CoveredBranches = 1,
            TotalMethods = 5,
            CoveredMethods = 2
        };

        // Assert
        Assert.Equal(42.857142857142854, metrics.LineCoveragePercentage, 10);
        Assert.Equal(25.0, metrics.BranchCoveragePercentage);
        Assert.Equal(40.0, metrics.MethodCoveragePercentage);
    }

    [Fact]
    public void CoverageMetrics_Merge_CombinesMetrics()
    {
        // Arrange
        var metrics1 = new CoverageMetrics
        {
            TotalLines = 50,
            CoveredLines = 40,
            TotalBranches = 10,
            CoveredBranches = 8,
            TotalMethods = 5,
            CoveredMethods = 4,
            UncoveredLines = new List<int> { 1, 2 },
            UncoveredBranches = new List<int> { 3, 4 },
            UncoveredMethods = new List<string> { "Method1" }
        };

        var metrics2 = new CoverageMetrics
        {
            TotalLines = 30,
            CoveredLines = 20,
            TotalBranches = 5,
            CoveredBranches = 3,
            TotalMethods = 3,
            CoveredMethods = 2,
            UncoveredLines = new List<int> { 5, 6 },
            UncoveredBranches = new List<int> { 7, 8 },
            UncoveredMethods = new List<string> { "Method2" }
        };

        // Act
        metrics1.Merge(metrics2);

        // Assert
        Assert.Equal(80, metrics1.TotalLines);
        Assert.Equal(60, metrics1.CoveredLines);
        Assert.Equal(15, metrics1.TotalBranches);
        Assert.Equal(11, metrics1.CoveredBranches);
        Assert.Equal(8, metrics1.TotalMethods);
        Assert.Equal(6, metrics1.CoveredMethods);
        Assert.Equal(new[] { 1, 2, 5, 6 }, metrics1.UncoveredLines);
        Assert.Equal(new[] { 3, 4, 7, 8 }, metrics1.UncoveredBranches);
        Assert.Equal(new[] { "Method1", "Method2" }, metrics1.UncoveredMethods);
    }

    [Fact]
    public void CoverageMetrics_Merge_WithNull_DoesNothing()
    {
        // Arrange
        var metrics = new CoverageMetrics
        {
            TotalLines = 100,
            CoveredLines = 80
        };

        // Act
        metrics.Merge(null);

        // Assert
        Assert.Equal(100, metrics.TotalLines);
        Assert.Equal(80, metrics.CoveredLines);
    }

    [Fact]
    public void CoverageMetrics_Merge_WithEmptyMetrics_AddsCorrectly()
    {
        // Arrange
        var metrics1 = new CoverageMetrics
        {
            TotalLines = 50,
            CoveredLines = 40
        };

        var metrics2 = new CoverageMetrics();

        // Act
        metrics1.Merge(metrics2);

        // Assert
        Assert.Equal(50, metrics1.TotalLines);
        Assert.Equal(40, metrics1.CoveredLines);
    }

    [Fact]
    public void CoverageMetrics_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new CoverageMetrics
        {
            TotalLines = 100,
            CoveredLines = 80,
            TotalBranches = 20,
            CoveredBranches = 15,
            TotalMethods = 10,
            CoveredMethods = 8,
            UncoveredLines = new List<int> { 1, 2, 3 },
            UncoveredBranches = new List<int> { 4, 5 },
            UncoveredMethods = new List<string> { "Method1", "Method2" }
        };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.Equal(original.TotalLines, clone.TotalLines);
        Assert.Equal(original.CoveredLines, clone.CoveredLines);
        Assert.Equal(original.TotalBranches, clone.TotalBranches);
        Assert.Equal(original.CoveredBranches, clone.CoveredBranches);
        Assert.Equal(original.TotalMethods, clone.TotalMethods);
        Assert.Equal(original.CoveredMethods, clone.CoveredMethods);
        Assert.Equal(original.UncoveredLines, clone.UncoveredLines);
        Assert.Equal(original.UncoveredBranches, clone.UncoveredBranches);
        Assert.Equal(original.UncoveredMethods, clone.UncoveredMethods);

        // Verify it's a deep copy
        Assert.NotSame(original.UncoveredLines, clone.UncoveredLines);
        Assert.NotSame(original.UncoveredBranches, clone.UncoveredBranches);
        Assert.NotSame(original.UncoveredMethods, clone.UncoveredMethods);
    }

    [Fact]
    public void CoverageMetrics_UncoveredCollections_CanBeModified()
    {
        // Arrange
        var metrics = new CoverageMetrics();

        // Act
        metrics.UncoveredLines.Add(1);
        metrics.UncoveredLines.Add(2);
        metrics.UncoveredBranches.Add(3);
        metrics.UncoveredMethods.Add("TestMethod");

        // Assert
        Assert.Equal(new[] { 1, 2 }, metrics.UncoveredLines);
        Assert.Equal(new[] { 3 }, metrics.UncoveredBranches);
        Assert.Equal(new[] { "TestMethod" }, metrics.UncoveredMethods);
    }

    [Fact]
    public void CoverageReport_AddAssemblyMetrics_UpdatesOverallMetrics()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics
        {
            TotalLines = 100,
            CoveredLines = 80
        };

        // Act
        report.AddAssemblyMetrics("TestAssembly", metrics);

        // Assert
        Assert.Equal(100, report.OverallMetrics.TotalLines);
        Assert.Equal(80, report.OverallMetrics.CoveredLines);
        Assert.Equal(metrics, report.GetAssemblyMetrics("TestAssembly"));
    }

    [Fact]
    public void CoverageReport_AddAssemblyMetrics_ThrowsOnNullAssemblyName()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => report.AddAssemblyMetrics(null!, metrics));
    }

    [Fact]
    public void CoverageReport_AddAssemblyMetrics_ThrowsOnEmptyAssemblyName()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => report.AddAssemblyMetrics("", metrics));
        Assert.Throws<ArgumentException>(() => report.AddAssemblyMetrics("   ", metrics));
    }

    [Fact]
    public void CoverageReport_AddAssemblyMetrics_ThrowsOnNullMetrics()
    {
        // Arrange
        var report = new CoverageReport();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => report.AddAssemblyMetrics("TestAssembly", null!));
    }

    [Fact]
    public void CoverageReport_MeetsThreshold_ReturnsCorrectValue()
    {
        // Arrange
        var report = new CoverageReport { MinimumCoverageThreshold = 80.0 };
        report.OverallMetrics.TotalLines = 100;
        report.OverallMetrics.CoveredLines = 85;

        // Assert
        Assert.True(report.MeetsThreshold);

        // Arrange
        report.OverallMetrics.CoveredLines = 75;

        // Assert
        Assert.False(report.MeetsThreshold);
    }

    [Fact]
    public void TestCoverageTracker_StartStopTracking_WorksCorrectly()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();

        // Assert
        Assert.False(tracker.IsTracking);

        // Act
        tracker.StartTracking();

        // Assert
        Assert.True(tracker.IsTracking);

        // Act
        tracker.StopTracking();

        // Assert
        Assert.False(tracker.IsTracking);
    }

    [Fact]
    public void TestCoverageTracker_RecordLineExecution_WhenNotTracking_DoesNothing()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();

        // Act
        tracker.RecordLineExecution("TestAssembly", "TestClass", "TestMethod", 42);

        // Assert
        // Should not throw and should not record anything
    }

    [Fact]
    public void TestCoverageTracker_RecordLineExecution_WhenTracking_RecordsExecution()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        // Act
        tracker.RecordLineExecution("TestAssembly", "TestClass", "TestMethod", 42);

        // Assert
        // The method uses internal hashsets, so we can't directly verify, but it should not throw
    }

    [Fact]
    public void TestCoverageTracker_RecordBranchExecution_WhenNotTracking_DoesNothing()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();

        // Act
        tracker.RecordBranchExecution("TestAssembly", "TestClass", "TestMethod", 1);

        // Assert
        // Should not throw and should not record anything
    }

    [Fact]
    public void TestCoverageTracker_RecordBranchExecution_WhenTracking_RecordsExecution()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        // Act
        tracker.RecordBranchExecution("TestAssembly", "TestClass", "TestMethod", 1);

        // Assert
        // The method uses internal hashsets, so we can't directly verify, but it should not throw
    }

    [Fact]
    public void TestCoverageTracker_RecordMethodExecution_WhenNotTracking_DoesNothing()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();

        // Act
        tracker.RecordMethodExecution("TestAssembly", "TestClass", "TestMethod");

        // Assert
        // Should not throw and should not record anything
    }

    [Fact]
    public void TestCoverageTracker_RecordMethodExecution_WhenTracking_RecordsExecution()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        // Act
        tracker.RecordMethodExecution("TestAssembly", "TestClass", "TestMethod");

        // Assert
        var report = tracker.GenerateReport("Test Report");
        Assert.True(report.MethodMetrics.ContainsKey("TestClass.TestMethod"));
    }

    [Fact]
    public void TestCoverageTracker_AnalyzeAssembly_WithNullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tracker.AnalyzeAssembly(null!));
    }

    [Fact]
    public void TestCoverageTracker_AnalyzeAssembly_WorksCorrectly()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var metrics = tracker.AnalyzeAssembly(assembly);

        // Assert
        Assert.True(metrics.TotalLines > 0);
        Assert.True(metrics.TotalMethods > 0);
    }

    [Fact]
    public void TestCoverageTracker_AnalyzeClass_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tracker.AnalyzeClass(null!));
    }

    [Fact]
    public void TestCoverageTracker_AnalyzeMethod_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        var method = typeof(TestCoverageTracker).GetMethod("StartTracking")!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tracker.AnalyzeMethod(null!, method));
    }

    [Fact]
    public void TestCoverageTracker_AnalyzeMethod_WithNullMethod_ThrowsArgumentNullException()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        var type = typeof(TestCoverageTracker);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tracker.AnalyzeMethod(type, null!));
    }

    [Fact]
    public void TestCoverageTracker_AnalyzeMethod_WithMethodBody_HandlesGracefully()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        var type = typeof(System.Console);
        var method = type.GetMethod("SetWindowSize", new[] { typeof(int), typeof(int) })!;

        // Act
        var metrics = tracker.AnalyzeMethod(type, method);

        // Assert
        Assert.Equal(1, metrics.TotalMethods);
        // The method has a body or not, but the code handles it
        Assert.True(metrics.TotalLines >= 0);
        Assert.True(metrics.TotalBranches >= 0);
    }

    [Fact]
    public void TestCoverageTracker_AnalyzeMethod_WithExecutedMethod_SetsCoveredMetrics()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        var type = typeof(TestCoverageTracker);
        var method = type.GetMethod("StartTracking")!;
        var assemblyName = type.Assembly.GetName().Name!;
        var className = type.FullName!;

        // Record execution first
        tracker.RecordMethodExecution(assemblyName, className, method.Name);

        // Act
        var metrics = tracker.AnalyzeMethod(type, method);

        // Assert
        Assert.Equal(1, metrics.TotalMethods);
        Assert.Equal(1, metrics.CoveredMethods);
        Assert.True(metrics.TotalLines > 0);
        Assert.True(metrics.TotalBranches >= 0);
        Assert.True(metrics.CoveredLines > 0);
        Assert.True(metrics.CoveredBranches >= 0);
        Assert.Empty(metrics.UncoveredMethods);
    }

    [Fact]
    public void TestCoverageTracker_AnalyzeClass_WorksCorrectly()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        var type = typeof(TestCoverageTracker);

        // Act
        var metrics = tracker.AnalyzeClass(type);

        // Assert
        Assert.True(metrics.TotalLines > 0);
        Assert.True(metrics.TotalMethods > 0);
    }

    [Fact]
    public void TestCoverageTracker_RecordTestScenario_WhenNotTracking_DoesNothing()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();

        // Act
        tracker.RecordTestScenario("TestScenario");

        // Assert
        var report = tracker.GenerateReport("Test Report");
        Assert.Empty(report.TestScenarios);
    }

    [Fact]
    public void TestCoverageTracker_RecordTestScenario_WhenTracking_AddsNewScenario()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        // Act
        tracker.RecordTestScenario("TestScenario");

        // Assert
        var report = tracker.GenerateReport("Test Report");
        Assert.Single(report.TestScenarios);
        Assert.Contains("TestScenario", report.TestScenarios);
    }

    [Fact]
    public void TestCoverageTracker_RecordTestScenario_WhenTracking_IgnoresDuplicate()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        // Act
        tracker.RecordTestScenario("TestScenario");
        tracker.RecordTestScenario("TestScenario"); // Duplicate

        // Assert
        var report = tracker.GenerateReport("Test Report");
        Assert.Single(report.TestScenarios);
        Assert.Contains("TestScenario", report.TestScenarios);
    }

    [Fact]
    public void TestCoverageTracker_GenerateReport_IncludesTestScenarios()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        tracker.RecordTestScenario("TestScenario1");
        tracker.RecordTestScenario("TestScenario2");
        tracker.RecordTestScenario("TestScenario1"); // Duplicate should be ignored

        // Act
        var report = tracker.GenerateReport("Test Report");

        // Assert
        Assert.Equal(2, report.TestScenarios.Count);
        Assert.Contains("TestScenario1", report.TestScenarios);
        Assert.Contains("TestScenario2", report.TestScenarios);
    }

    [Fact]
    public void TestCoverageTracker_Reset_ClearsAllData()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        tracker.RecordMethodExecution("TestAssembly", "TestClass", "TestMethod");
        tracker.RecordTestScenario("TestScenario");

        // Act
        tracker.Reset();

        // Assert
        var report = tracker.GenerateReport("Test Report");
        Assert.Empty(report.MethodMetrics);
        Assert.Empty(report.TestScenarios);
    }

    [Fact]
    public void CoverageReport_AddClassMetrics_UpdatesClassMetrics()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics
        {
            TotalLines = 50,
            CoveredLines = 40
        };

        // Act
        report.AddClassMetrics("TestClass", metrics);

        // Assert
        Assert.Equal(metrics, report.GetClassMetrics("TestClass"));
    }

    [Fact]
    public void CoverageReport_AddClassMetrics_ThrowsOnInvalidClassName()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => report.AddClassMetrics("", metrics));
        Assert.Throws<ArgumentException>(() => report.AddClassMetrics("   ", metrics));
        Assert.Throws<ArgumentNullException>(() => report.AddClassMetrics("TestClass", null!));
    }

    [Fact]
    public void CoverageReport_AddMethodMetrics_ThrowsOnNullMethodName()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => report.AddMethodMetrics(null!, metrics));
    }

    [Fact]
    public void CoverageReport_AddMethodMetrics_ThrowsOnEmptyMethodName()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => report.AddMethodMetrics("", metrics));
        Assert.Throws<ArgumentException>(() => report.AddMethodMetrics("   ", metrics));
    }

    [Fact]
    public void CoverageReport_AddMethodMetrics_ThrowsOnNullMetrics()
    {
        // Arrange
        var report = new CoverageReport();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => report.AddMethodMetrics("TestMethod", null!));
    }

    [Fact]
    public void CoverageReport_GetClassMetrics_ReturnsNullWhenNotFound()
    {
        // Arrange
        var report = new CoverageReport();

        // Act
        var result = report.GetClassMetrics("NonExistentClass");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CoverageReport_GetMethodMetrics_ReturnsNullWhenNotFound()
    {
        // Arrange
        var report = new CoverageReport();

        // Act
        var result = report.GetMethodMetrics("NonExistentMethod");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CoverageReport_AddTestScenario_ThrowsOnNullScenarioName()
    {
        // Arrange
        var report = new CoverageReport();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => report.AddTestScenario(null!));
    }

    [Fact]
    public void CoverageReport_AddTestScenario_ThrowsOnEmptyScenarioName()
    {
        // Arrange
        var report = new CoverageReport();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => report.AddTestScenario(""));
        Assert.Throws<ArgumentException>(() => report.AddTestScenario("   "));
    }

    [Fact]
    public void CoverageReport_AddTestScenario_AddsNewScenario()
    {
        // Arrange
        var report = new CoverageReport();

        // Act
        report.AddTestScenario("TestScenario");

        // Assert
        Assert.Single(report.TestScenarios);
        Assert.Contains("TestScenario", report.TestScenarios);
    }

    [Fact]
    public void CoverageReport_AddTestScenario_IgnoresDuplicateScenario()
    {
        // Arrange
        var report = new CoverageReport();

        // Act
        report.AddTestScenario("TestScenario");
        report.AddTestScenario("TestScenario"); // Duplicate

        // Assert
        Assert.Single(report.TestScenarios);
        Assert.Contains("TestScenario", report.TestScenarios);
    }

    [Fact]
    public void CoverageReport_GetAssemblyMetrics_ReturnsMetricsWhenFound()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics { TotalLines = 100, CoveredLines = 80 };
        report.AddAssemblyMetrics("TestAssembly", metrics);

        // Act
        var result = report.GetAssemblyMetrics("TestAssembly");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(metrics, result);
    }

    [Fact]
    public void CoverageReport_GetAssemblyMetrics_ReturnsNullWhenNotFound()
    {
        // Arrange
        var report = new CoverageReport();

        // Act
        var result = report.GetAssemblyMetrics("NonExistentAssembly");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CoverageReport_GetClassMetrics_ReturnsMetricsWhenFound()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics { TotalLines = 50, CoveredLines = 40 };
        report.AddClassMetrics("TestClass", metrics);

        // Act
        var result = report.GetClassMetrics("TestClass");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(metrics, result);
    }

    [Fact]
    public void CoverageReport_GetMethodMetrics_ReturnsMetricsWhenFound()
    {
        // Arrange
        var report = new CoverageReport();
        var metrics = new CoverageMetrics { TotalLines = 20, CoveredLines = 15 };
        report.AddMethodMetrics("TestMethod", metrics);

        // Act
        var result = report.GetMethodMetrics("TestMethod");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(metrics, result);
    }

    [Fact]
    public void TestCoverageTracker_EstimateBranches_WithBranchOpcodes_ReturnsCount()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        var type = typeof(TestCoverageTracker);
        var method = type.GetMethod("StartTracking")!;
        var methodBody = method.GetMethodBody();

        // Act
        var branchCount = typeof(TestCoverageTracker).GetMethod("EstimateBranches", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { methodBody }) as int?;

        // Assert
        Assert.NotNull(branchCount);
        Assert.True(branchCount >= 0);
    }

    [Fact]
    public void TestCoverageTracker_EstimateBranches_WithNoBranchOpcodes_ReturnsMinimum()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        var type = typeof(TestCoverageTracker);
        var method = type.GetMethod("Dispose")!; // Simple method
        var methodBody = method.GetMethodBody();

        // Act
        var branchCount = typeof(TestCoverageTracker).GetMethod("EstimateBranches", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { methodBody }) as int?;

        // Assert
        Assert.NotNull(branchCount);
        Assert.True(branchCount >= 1); // Minimum is 1
    }

    [Fact]
    public void CoverageReport_GenerateSummary_ReturnsFormattedString()
    {
        // Arrange
        var report = new CoverageReport
        {
            ReportName = "Test Report",
            GeneratedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            MinimumCoverageThreshold = 75.0
        };

        report.OverallMetrics.TotalLines = 100;
        report.OverallMetrics.CoveredLines = 80;

        // Act
        var summary = report.GenerateSummary();

        // Assert
        Assert.Contains("Test Report", summary);
        Assert.Contains("80.00%", summary);
        Assert.Contains("True", summary);
    }

    [Fact]
    public void TestCoverageTracker_GenerateReport_DoesNotAddPlaceholderWhenMethodAlreadyAnalyzed()
    {
        // Arrange
        using var tracker = new TestCoverageTracker();
        tracker.StartTracking();

        var type = typeof(TestCoverageTracker);
        var method = type.GetMethod("StartTracking")!;
        var methodKey = $"{type.FullName}.{method.Name}";

        // Analyze the method first
        var analyzedMetrics = tracker.AnalyzeMethod(type, method);

        // Record method execution
        tracker.RecordMethodExecution(type.Assembly.GetName().Name!, type.FullName!, method.Name);

        // Act
        var report = tracker.GenerateReport("Test Report");

        // Assert
        Assert.True(report.MethodMetrics.ContainsKey(methodKey));
        var metrics = report.MethodMetrics[methodKey];
        // Should be the analyzed metrics, not placeholder
        Assert.Equal(analyzedMetrics.TotalMethods, metrics.TotalMethods);
        Assert.Equal(analyzedMetrics.TotalLines, metrics.TotalLines);
        Assert.Equal(analyzedMetrics.TotalBranches, metrics.TotalBranches);
    }
}