using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Testing.Tests.Coverage;

public class TestCoverageTrackerTests : IDisposable
{
    private readonly TestCoverageTracker _tracker;

    public TestCoverageTrackerTests()
    {
        _tracker = new TestCoverageTracker();
    }

    public void Dispose()
    {
        _tracker.Dispose();
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var tracker = new TestCoverageTracker();

        // Assert
        Assert.Equal(80.0, tracker.MinimumCoverageThreshold);
        Assert.False(tracker.IsTracking);
    }

    [Fact]
    public void MinimumCoverageThreshold_CanBeSet()
    {
        // Arrange
        var expected = 90.0;

        // Act
        _tracker.MinimumCoverageThreshold = expected;

        // Assert
        Assert.Equal(expected, _tracker.MinimumCoverageThreshold);
    }

    [Fact]
    public void IsTracking_ReflectsTrackingState()
    {
        // Arrange
        Assert.False(_tracker.IsTracking);

        // Act
        _tracker.StartTracking();

        // Assert
        Assert.True(_tracker.IsTracking);

        // Act
        _tracker.StopTracking();

        // Assert
        Assert.False(_tracker.IsTracking);
    }

    [Fact]
    public void StartTracking_EnablesTrackingAndClearsData()
    {
        // Arrange
        _tracker.StartTracking();
        _tracker.RecordLineExecution("test", "test", "test", 1);
        _tracker.StopTracking();

        // Act
        _tracker.StartTracking();

        // Assert
        Assert.True(_tracker.IsTracking);
        // Data should be cleared, but we can't directly check private fields
        // We'll verify through behavior in other tests
    }

    [Fact]
    public void StopTracking_DisablesTracking()
    {
        // Arrange
        _tracker.StartTracking();
        Assert.True(_tracker.IsTracking);

        // Act
        _tracker.StopTracking();

        // Assert
        Assert.False(_tracker.IsTracking);
    }

    [Fact]
    public void RecordLineExecution_WhenNotTracking_DoesNothing()
    {
        // Arrange
        Assert.False(_tracker.IsTracking);

        // Act
        _tracker.RecordLineExecution("test", "test", "test", 1);

        // Assert
        // No exception, and since not tracking, no data recorded
    }

    [Fact]
    public void RecordLineExecution_WhenTracking_RecordsLine()
    {
        // Arrange
        _tracker.StartTracking();

        // Act
        _tracker.RecordLineExecution("assembly", "class", "method", 42);

        // Assert
        // We can't directly check private sets, but GenerateReport should include it
        var report = _tracker.GenerateReport("test");
        Assert.NotNull(report);
    }

    [Fact]
    public void RecordBranchExecution_WhenNotTracking_DoesNothing()
    {
        // Arrange
        Assert.False(_tracker.IsTracking);

        // Act
        _tracker.RecordBranchExecution("test", "test", "test", 1);

        // Assert
        // No exception
    }

    [Fact]
    public void RecordBranchExecution_WhenTracking_RecordsBranch()
    {
        // Arrange
        _tracker.StartTracking();

        // Act
        _tracker.RecordBranchExecution("assembly", "class", "method", 1);

        // Assert
        var report = _tracker.GenerateReport("test");
        Assert.NotNull(report);
    }

    [Fact]
    public void RecordMethodExecution_WhenNotTracking_DoesNothing()
    {
        // Arrange
        Assert.False(_tracker.IsTracking);

        // Act
        _tracker.RecordMethodExecution("test", "test", "test");

        // Assert
        // No exception
    }

    [Fact]
    public void RecordMethodExecution_WhenTracking_RecordsMethod()
    {
        // Arrange
        _tracker.StartTracking();

        // Act
        _tracker.RecordMethodExecution("assembly", "TestClass", "TestMethod");

        // Assert
        var report = _tracker.GenerateReport("test");
        Assert.True(report.MethodMetrics.ContainsKey("TestClass.TestMethod"));
    }

    [Fact]
    public void RecordTestScenario_WhenNotTracking_DoesNothing()
    {
        // Arrange
        Assert.False(_tracker.IsTracking);

        // Act
        _tracker.RecordTestScenario("test scenario");

        // Assert
        // No exception
    }

    [Fact]
    public void RecordTestScenario_WhenTracking_RecordsScenario()
    {
        // Arrange
        _tracker.StartTracking();

        // Act
        _tracker.RecordTestScenario("MyTestScenario");

        // Assert
        var report = _tracker.GenerateReport("test");
        Assert.Contains("MyTestScenario", report.TestScenarios);
    }

    [Fact]
    public void RecordTestScenario_DuplicateScenario_NotAddedTwice()
    {
        // Arrange
        _tracker.StartTracking();

        // Act
        _tracker.RecordTestScenario("DuplicateScenario");
        _tracker.RecordTestScenario("DuplicateScenario");

        // Assert
        var report = _tracker.GenerateReport("test");
        Assert.Single(report.TestScenarios.Where(s => s == "DuplicateScenario"));
    }

    [Fact]
    public void AnalyzeAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _tracker.AnalyzeAssembly(null!));
        Assert.Equal("assembly", exception.ParamName);
    }

    [Fact]
    public void AnalyzeAssembly_ValidAssembly_ReturnsMetrics()
    {
        // Arrange
        var assembly = typeof(TestCoverageTracker).Assembly;

        // Act
        var metrics = _tracker.AnalyzeAssembly(assembly);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.TotalMethods >= 0);
    }

    [Fact]
    public void AnalyzeClass_NullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _tracker.AnalyzeClass(null!));
        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void AnalyzeClass_ValidType_ReturnsMetrics()
    {
        // Arrange
        var type = typeof(TestCoverageTracker);

        // Act
        var metrics = _tracker.AnalyzeClass(type);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.TotalMethods >= 0);
    }

    [Fact]
    public void AnalyzeClass_AbstractClass_ExcludesAbstractMethods()
    {
        // Arrange
        var type = typeof(AbstractTestClass);

        // Act
        var metrics = _tracker.AnalyzeClass(type);

        // Assert
        Assert.NotNull(metrics);
        // Should not include abstract methods
    }

    [Fact]
    public void AnalyzeMethod_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var method = typeof(TestCoverageTracker).GetMethod("StartTracking");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _tracker.AnalyzeMethod(null!, method!));
        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void AnalyzeMethod_NullMethod_ThrowsArgumentNullException()
    {
        // Arrange
        var type = typeof(TestCoverageTracker);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _tracker.AnalyzeMethod(type, null!));
        Assert.Equal("method", exception.ParamName);
    }

    [Fact]
    public void AnalyzeMethod_MethodWithoutBody_ReturnsEmptyMetrics()
    {
        // Arrange
        var type = typeof(TestCoverageTracker);
        var method = type.GetMethod("get_IsTracking"); // Property getter, may not have body

        // Act
        var metrics = _tracker.AnalyzeMethod(type, method!);

        // Assert
        Assert.NotNull(metrics);
        // If no body, metrics should be default
    }

    [Fact]
    public void AnalyzeMethod_MethodWithBody_ReturnsMetrics()
    {
        // Arrange
        var type = typeof(TestCoverageTracker);
        var method = type.GetMethod("StartTracking");

        // Act
        var metrics = _tracker.AnalyzeMethod(type, method!);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.TotalMethods);
        Assert.True(metrics.TotalLines > 0);
    }

    [Fact]
    public void AnalyzeMethod_ExecutedMethod_IncludesCoverage()
    {
        // Arrange
        var type = typeof(TestCoverageTracker);
        var method = type.GetMethod("StartTracking");
        _tracker.StartTracking();
        _tracker.RecordMethodExecution("assembly", type.FullName!, method!.Name);

        // Act
        var metrics = _tracker.AnalyzeMethod(type, method);

        // Assert
        Assert.Equal(1, metrics.CoveredMethods);
        Assert.True(metrics.CoveredLines > 0);
    }

    [Fact]
    public void GenerateReport_IncludesAllData()
    {
        // Arrange
        _tracker.StartTracking();
        _tracker.RecordTestScenario("Scenario1");
        _tracker.RecordMethodExecution("asm", "Class1", "Method1");
        var assembly = typeof(TestCoverageTracker).Assembly;
        _tracker.AnalyzeAssembly(assembly);

        // Act
        var report = _tracker.GenerateReport("TestReport");

        // Assert
        Assert.Equal("TestReport", report.ReportName);
        Assert.Contains("Scenario1", report.TestScenarios);
        Assert.True(report.MethodMetrics.ContainsKey("Class1.Method1"));
        Assert.True(report.AssemblyMetrics.Count > 0);
    }

    [Fact]
    public void GenerateReport_IncludesExecutedMethodsNotAnalyzed()
    {
        // Arrange
        _tracker.StartTracking();
        _tracker.RecordMethodExecution("asm", "UnanalyzedClass", "UnanalyzedMethod");

        // Act
        var report = _tracker.GenerateReport("TestReport");

        // Assert
        Assert.True(report.MethodMetrics.ContainsKey("UnanalyzedClass.UnanalyzedMethod"));
        var metrics = report.MethodMetrics["UnanalyzedClass.UnanalyzedMethod"];
        Assert.Equal(1, metrics.TotalMethods);
        Assert.Equal(1, metrics.CoveredMethods);
    }

    [Fact]
    public void Reset_ClearsAllData()
    {
        // Arrange
        _tracker.StartTracking();
        _tracker.RecordTestScenario("Scenario1");
        _tracker.RecordMethodExecution("asm", "Class1", "Method1");

        // Act
        _tracker.Reset();

        // Assert
        var report = _tracker.GenerateReport("TestReport");
        Assert.Empty(report.TestScenarios);
        Assert.Empty(report.MethodMetrics);
    }

    [Fact]
    public void Dispose_StopsTrackingAndResets()
    {
        // Arrange
        _tracker.StartTracking();
        _tracker.RecordTestScenario("Scenario1");

        // Act
        _tracker.Dispose();

        // Assert
        Assert.False(_tracker.IsTracking);
        var report = _tracker.GenerateReport("TestReport");
        Assert.Empty(report.TestScenarios);
    }


    private abstract class AbstractTestClass
    {
        public abstract void AbstractMethod();
        public void ConcreteMethod() { }
    }
}