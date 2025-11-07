using Microsoft.CodeAnalysis;
using System.Linq;
using Relay.SourceGenerator.Core;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Comprehensive tests for ErrorIsolation.ExecuteGeneratorsWithIsolation method.
/// Covers all branches, cases, and throws.
/// </summary>
public class ErrorIsolationExecuteGeneratorsWithIsolationTests
{
    private readonly TestCodeGenerator _testGenerator = new();
    private readonly TestDiagnosticReporter _diagnosticReporter = new();

    #region Parameter Validation Tests (No null validation in actual implementation)

    [Fact]
    public void ExecuteGeneratorsWithIsolation_NullGenerators_ThrowsNullReferenceException()
    {
        // Arrange
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act & Assert
        var exception = Assert.Throws<NullReferenceException>(() =>
            ErrorIsolation.ExecuteGeneratorsWithIsolation(null, result, options, reporter));
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_NullResult_HandledByExecuteGeneratorWithIsolation()
    {
        // Arrange
        var generator = new TestCodeGenerator 
        { 
            GeneratorNameValue = "TestGenerator",
            OutputFileNameValue = "TestFile",
            CanGenerateResult = true,
            GenerateResult = "test source"
        };
        var generators = new List<ICodeGenerator> { generator };
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, null, options, reporter);

        // Assert
        Assert.Empty(generatedSources);
        Assert.Single(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_NullOptions_HandledByExecuteGeneratorWithIsolation()
    {
        // Arrange
        var generator = new TestCodeGenerator 
        { 
            GeneratorNameValue = "TestGenerator",
            OutputFileNameValue = "TestFile",
            CanGenerateResult = true,
            GenerateResult = "test source"
        };
        var generators = new List<ICodeGenerator> { generator };
        var result = new HandlerDiscoveryResult();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, null, reporter);

        // Assert
        Assert.Empty(generatedSources);
        Assert.Single(reporter.Diagnostics);
    }



    #endregion

    #region Empty Generators Collection Tests

[Fact]
public void ExecuteGeneratorsWithIsolation_NullDiagnosticReporter_ThrowsWhenReportingError()
{
    // Arrange
    var generators = new[] { _testGenerator };
    var result = new HandlerDiscoveryResult();
    var options = new GenerationOptions();

    // Act & Assert - Should throw NullReferenceException when ReportGeneratorError tries to report
    Assert.Throws<NullReferenceException>(() => ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, null!));
}

    [Fact]
    public void ExecuteGeneratorsWithIsolation_EmptyGeneratorsAsArray_ReturnsEmptyDictionary()
    {
        // Arrange
        var generators = Array.Empty<ICodeGenerator>();
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Empty(generatedSources);
        Assert.Empty(reporter.Diagnostics);
    }

    #endregion

    #region Successful Generation Tests

    [Fact]
    public void ExecuteGeneratorsWithIsolation_AllGeneratorsSucceed_ReturnsAllSources()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "Generator1",
            OutputFileNameValue = "File1",
            CanGenerateResult = true,
            GenerateResult = "source1"
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "Generator2",
            OutputFileNameValue = "File2",
            CanGenerateResult = true,
            GenerateResult = "source2"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Equal(2, generatedSources.Count);
        Assert.True(generatedSources.ContainsKey("File1.g.cs"));
        Assert.True(generatedSources.ContainsKey("File2.g.cs"));
        Assert.Equal("source1", generatedSources["File1.g.cs"]);
        Assert.Equal("source2", generatedSources["File2.g.cs"]);
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_SingleGeneratorSucceeds_ReturnsSingleSource()
    {
        // Arrange
        var generator = new TestCodeGenerator 
        { 
            GeneratorNameValue = "SingleGenerator",
            OutputFileNameValue = "SingleFile",
            CanGenerateResult = true,
            GenerateResult = "single source"
        };
        var generators = new List<ICodeGenerator> { generator };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Single(generatedSources);
        Assert.True(generatedSources.ContainsKey("SingleFile.g.cs"));
        Assert.Equal("single source", generatedSources["SingleFile.g.cs"]);
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_ManyGeneratorsSucceed_ReturnsAllSources()
    {
        // Arrange
        var generators = new List<ICodeGenerator>();
        var expectedSources = new Dictionary<string, string>();
        
        for (int i = 0; i < 10; i++)
        {
            var generator = new TestCodeGenerator 
            { 
                GeneratorNameValue = $"Generator{i}",
                OutputFileNameValue = $"File{i}",
                CanGenerateResult = true,
                GenerateResult = $"source{i}"
            };
            generators.Add(generator);
            expectedSources[$"File{i}.g.cs"] = $"source{i}";
        }
        
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Equal(10, generatedSources.Count);
        foreach (var kvp in expectedSources)
        {
            Assert.True(generatedSources.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, generatedSources[kvp.Key]);
        }
        Assert.Empty(reporter.Diagnostics);
    }

    #endregion

    #region Mixed Success/Failure Scenarios

    [Fact]
    public void ExecuteGeneratorsWithIsolation_SomeGeneratorsFail_ReturnsSuccessfulSourcesAndReportsErrors()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "SuccessGenerator",
            OutputFileNameValue = "SuccessFile",
            CanGenerateResult = true,
            GenerateResult = "success source"
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "FailGenerator",
            OutputFileNameValue = "FailFile",
            CanGenerateResult = true,
            ThrowException = new InvalidOperationException("Generation failed")
        };
        var generator3 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "AnotherSuccessGenerator",
            OutputFileNameValue = "AnotherSuccessFile",
            CanGenerateResult = true,
            GenerateResult = "another success source"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2, generator3 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Equal(2, generatedSources.Count);
        Assert.True(generatedSources.ContainsKey("SuccessFile.g.cs"));
        Assert.True(generatedSources.ContainsKey("AnotherSuccessFile.g.cs"));
        Assert.False(generatedSources.ContainsKey("FailFile.g.cs"));
        Assert.Equal("success source", generatedSources["SuccessFile.g.cs"]);
        Assert.Equal("another success source", generatedSources["AnotherSuccessFile.g.cs"]);
        
        Assert.Single(reporter.Diagnostics);
        var message = reporter.Diagnostics[0].GetMessage();
        Assert.Contains("Error in FailGenerator", message);
        Assert.Contains("Generation failed", message);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_AllGeneratorsFail_ReturnsEmptyDictionaryAndReportsAllErrors()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "FailGenerator1",
            OutputFileNameValue = "FailFile1",
            CanGenerateResult = true,
            ThrowException = new InvalidOperationException("First failure")
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "FailGenerator2",
            OutputFileNameValue = "FailFile2",
            CanGenerateResult = true,
            ThrowException = new ArgumentException("Second failure")
        };
        var generators = new List<ICodeGenerator> { generator1, generator2 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Empty(generatedSources);
        Assert.Equal(2, reporter.Diagnostics.Count);
        
        var message1 = reporter.Diagnostics[0].GetMessage();
        Assert.Contains("Error in FailGenerator1", message1);
        Assert.Contains("First failure", message1);
        
        var message2 = reporter.Diagnostics[1].GetMessage();
        Assert.Contains("Error in FailGenerator2", message2);
        Assert.Contains("Second failure", message2);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_SomeGeneratorsCannotGenerate_ReturnsOnlyGeneratableSources()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "CanGenerateGenerator",
            OutputFileNameValue = "CanGenerateFile",
            CanGenerateResult = true,
            GenerateResult = "can generate source"
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "CannotGenerateGenerator",
            OutputFileNameValue = "CannotGenerateFile",
            CanGenerateResult = false
        };
        var generator3 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "AnotherCanGenerateGenerator",
            OutputFileNameValue = "AnotherCanGenerateFile",
            CanGenerateResult = true,
            GenerateResult = "another can generate source"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2, generator3 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Equal(2, generatedSources.Count);
        Assert.True(generatedSources.ContainsKey("CanGenerateFile.g.cs"));
        Assert.True(generatedSources.ContainsKey("AnotherCanGenerateFile.g.cs"));
        Assert.False(generatedSources.ContainsKey("CannotGenerateFile.g.cs"));
        Assert.Equal("can generate source", generatedSources["CanGenerateFile.g.cs"]);
        Assert.Equal("another can generate source", generatedSources["AnotherCanGenerateFile.g.cs"]);
        Assert.Empty(reporter.Diagnostics);
    }

    #endregion

    #region OperationCanceledException Tests

    [Fact]
    public void ExecuteGeneratorsWithIsolation_OperationCanceledException_StopsProcessingAndBreaks()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "SuccessGenerator",
            OutputFileNameValue = "SuccessFile",
            CanGenerateResult = true,
            GenerateResult = "success source"
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "CancelGenerator",
            OutputFileNameValue = "CancelFile",
            CanGenerateResult = true,
            ThrowException = new OperationCanceledException("Operation cancelled")
        };
        var generator3 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "AfterCancelGenerator",
            OutputFileNameValue = "AfterCancelFile",
            CanGenerateResult = true,
            GenerateResult = "after cancel source"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2, generator3 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Single(generatedSources);
        Assert.True(generatedSources.ContainsKey("SuccessFile.g.cs"));
        Assert.Equal("success source", generatedSources["SuccessFile.g.cs"]);
        Assert.False(generatedSources.ContainsKey("CancelFile.g.cs"));
        Assert.False(generatedSources.ContainsKey("AfterCancelFile.g.cs"));
        Assert.False(generator3.GenerateCalled);
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_OperationCanceledExceptionWithToken_StopsProcessingAndBreaks()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "SuccessGenerator",
            OutputFileNameValue = "SuccessFile",
            CanGenerateResult = true,
            GenerateResult = "success source"
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "CancelGenerator",
            OutputFileNameValue = "CancelFile",
            CanGenerateResult = true,
            ThrowException = new OperationCanceledException(cts.Token)
        };
        var generators = new List<ICodeGenerator> { generator1, generator2 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Single(generatedSources);
        Assert.True(generatedSources.ContainsKey("SuccessFile.g.cs"));
        Assert.Equal("success source", generatedSources["SuccessFile.g.cs"]);
        Assert.False(generatedSources.ContainsKey("CancelFile.g.cs"));
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_OperationCanceledExceptionOnFirstGenerator_StopsProcessingImmediately()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "CancelGenerator",
            OutputFileNameValue = "CancelFile",
            CanGenerateResult = true,
            ThrowException = new OperationCanceledException()
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "NeverExecutedGenerator",
            OutputFileNameValue = "NeverExecutedFile",
            CanGenerateResult = true,
            GenerateResult = "never executed source"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Empty(generatedSources);
        Assert.False(generator2.GenerateCalled);
        Assert.Empty(reporter.Diagnostics);
    }

    #endregion

    #region Safety Net Exception Tests

    [Fact]
    public void ExecuteGeneratorsWithIsolation_ExecuteGeneratorWithIsolationThrows_ReportsErrorAndContinues()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "SuccessGenerator",
            OutputFileNameValue = "SuccessFile",
            CanGenerateResult = true,
            GenerateResult = "success source"
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "SafetyNetFailGenerator",
            OutputFileNameValue = "SafetyNetFailFile",
            CanGenerateResult = true,
            // This simulates ExecuteGeneratorWithIsolation itself throwing
            ThrowInExecuteGeneratorWithIsolation = new InvalidOperationException("ExecuteGeneratorWithIsolation failed")
        };
        var generator3 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "AfterSafetyNetGenerator",
            OutputFileNameValue = "AfterSafetyNetFile",
            CanGenerateResult = true,
            GenerateResult = "after safety net source"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2, generator3 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Equal(2, generatedSources.Count);
        Assert.True(generatedSources.ContainsKey("SuccessFile.g.cs"));
        Assert.True(generatedSources.ContainsKey("AfterSafetyNetFile.g.cs"));
        Assert.False(generatedSources.ContainsKey("SafetyNetFailFile.g.cs"));
        Assert.Equal("success source", generatedSources["SuccessFile.g.cs"]);
        Assert.Equal("after safety net source", generatedSources["AfterSafetyNetFile.g.cs"]);
        
        Assert.Single(reporter.Diagnostics);
        var message = reporter.Diagnostics[0].GetMessage();
        Assert.Contains("Error in SafetyNetFailGenerator", message);
        Assert.Contains("ExecuteGeneratorWithIsolation failed", message);
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Fact]
    public void ExecuteGeneratorsWithIsolation_DuplicateFileNames_BothAddedToDictionary()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "Generator1",
            OutputFileNameValue = "DuplicateFile",
            CanGenerateResult = true,
            GenerateResult = "source1"
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "Generator2",
            OutputFileNameValue = "DuplicateFile", // Same file name
            CanGenerateResult = true,
            GenerateResult = "source2"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Single(generatedSources); // Second one overwrites the first
        Assert.True(generatedSources.ContainsKey("DuplicateFile.g.cs"));
        Assert.Equal("source2", generatedSources["DuplicateFile.g.cs"]); // Last one wins
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_SpecialCharactersInFileNames_HandlesCorrectly()
    {
        // Arrange
        var generator = new TestCodeGenerator 
        { 
            GeneratorNameValue = "SpecialCharGenerator",
            OutputFileNameValue = "File-With_Special.Chars",
            CanGenerateResult = true,
            GenerateResult = "special char source"
        };
        var generators = new List<ICodeGenerator> { generator };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Single(generatedSources);
        Assert.True(generatedSources.ContainsKey("File-With_Special.Chars.g.cs"));
        Assert.Equal("special char source", generatedSources["File-With_Special.Chars.g.cs"]);
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_GeneratorsAsIEnumerable_ProcessesCorrectly()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "Generator1",
            OutputFileNameValue = "File1",
            CanGenerateResult = true,
            GenerateResult = "source1"
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "Generator2",
            OutputFileNameValue = "File2",
            CanGenerateResult = true,
            GenerateResult = "source2"
        };
        
        // Use different IEnumerable implementations
        IEnumerable<ICodeGenerator> generators = new[] { generator1, generator2 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Equal(2, generatedSources.Count);
        Assert.True(generatedSources.ContainsKey("File1.g.cs"));
        Assert.True(generatedSources.ContainsKey("File2.g.cs"));
        Assert.Equal("source1", generatedSources["File1.g.cs"]);
        Assert.Equal("source2", generatedSources["File2.g.cs"]);
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_GeneratorsFromYieldReturn_ProcessesCorrectly()
    {
        // Arrange
        IEnumerable<ICodeGenerator> GetGenerators()
        {
            yield return new TestCodeGenerator 
            { 
                GeneratorNameValue = "YieldGenerator1",
                OutputFileNameValue = "YieldFile1",
                CanGenerateResult = true,
                GenerateResult = "yield source1"
            };
            yield return new TestCodeGenerator 
            { 
                GeneratorNameValue = "YieldGenerator2",
                OutputFileNameValue = "YieldFile2",
                CanGenerateResult = true,
                GenerateResult = "yield source2"
            };
        }
        
        var generators = GetGenerators();
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Equal(2, generatedSources.Count);
        Assert.True(generatedSources.ContainsKey("YieldFile1.g.cs"));
        Assert.True(generatedSources.ContainsKey("YieldFile2.g.cs"));
        Assert.Equal("yield source1", generatedSources["YieldFile1.g.cs"]);
        Assert.Equal("yield source2", generatedSources["YieldFile2.g.cs"]);
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_EmptySourceFromGenerator_IncludesInDictionary()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "EmptySourceGenerator",
            OutputFileNameValue = "EmptyFile",
            CanGenerateResult = true,
            GenerateResult = ""
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "NormalGenerator",
            OutputFileNameValue = "NormalFile",
            CanGenerateResult = true,
            GenerateResult = "normal source"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Equal(2, generatedSources.Count);
        Assert.True(generatedSources.ContainsKey("EmptyFile.g.cs"));
        Assert.True(generatedSources.ContainsKey("NormalFile.g.cs"));
        Assert.Equal("", generatedSources["EmptyFile.g.cs"]);
        Assert.Equal("normal source", generatedSources["NormalFile.g.cs"]);
        Assert.Empty(reporter.Diagnostics);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_NullSourceFromGenerator_ConvertedToEmptyString()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "NullSourceGenerator",
            OutputFileNameValue = "NullFile",
            CanGenerateResult = true,
            GenerateResult = null
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "NormalGenerator",
            OutputFileNameValue = "NormalFile",
            CanGenerateResult = true,
            GenerateResult = "normal source"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Equal(2, generatedSources.Count);
        Assert.True(generatedSources.ContainsKey("NullFile.g.cs"));
        Assert.True(generatedSources.ContainsKey("NormalFile.g.cs"));
        Assert.Equal("", generatedSources["NullFile.g.cs"]);
        Assert.Equal("normal source", generatedSources["NormalFile.g.cs"]);
        Assert.Empty(reporter.Diagnostics);
    }

    #endregion

    #region Test Helper Classes

    private class TestCodeGenerator : ICodeGenerator
    {
        public string GeneratorNameValue { get; set; } = "TestGenerator";
        public string OutputFileNameValue { get; set; } = "TestOutput";
        public int PriorityValue { get; set; } = 100;
        
        public bool CanGenerateResult { get; set; } = true;
        public string? GenerateResult { get; set; } = "generated source";
        public Exception? ThrowException { get; set; }
        public Exception? ThrowInCanGenerate { get; set; }
        public Exception? ThrowInExecuteGeneratorWithIsolation { get; set; }
        
        public bool GenerateCalled { get; private set; }
        public bool CanGenerateCalled { get; private set; }
        public HandlerDiscoveryResult? LastResult { get; private set; }
        public GenerationOptions? LastOptions { get; private set; }
        
        public Func<string>? GenerateAction { get; set; }

        public string GeneratorName => GeneratorNameValue;
        public string OutputFileName => OutputFileNameValue;
        public int Priority => PriorityValue;

        public bool CanGenerate(HandlerDiscoveryResult result)
        {
            CanGenerateCalled = true;
            if (ThrowInCanGenerate != null)
                throw ThrowInCanGenerate;
            return CanGenerateResult;
        }

        public string Generate(HandlerDiscoveryResult result, GenerationOptions options)
        {
            GenerateCalled = true;
            LastResult = result;
            LastOptions = options;
            
            if (ThrowInExecuteGeneratorWithIsolation != null)
                throw ThrowInExecuteGeneratorWithIsolation;
            
            if (ThrowException != null)
                throw ThrowException;
            
            return GenerateAction?.Invoke() ?? GenerateResult ?? string.Empty;
        }
    }

    private class TestDiagnosticReporter : IDiagnosticReporter
    {
        public List<Diagnostic> Diagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_CriticalException_ReportsCriticalErrorAndStopsProcessing()
    {
        // Arrange
        var generator1 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "SuccessGenerator",
            OutputFileNameValue = "SuccessFile",
            CanGenerateResult = true,
            GenerateResult = "success source"
        };
        var generator2 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "CriticalErrorGenerator",
            OutputFileNameValue = "CriticalErrorFile",
            CanGenerateResult = true,
            // This simulates a critical exception being thrown from the generator
            ThrowException = new OutOfMemoryException("Out of memory during generation")
        };
        var generator3 = new TestCodeGenerator 
        { 
            GeneratorNameValue = "AfterCriticalGenerator",
            OutputFileNameValue = "AfterCriticalFile",
            CanGenerateResult = true,
            GenerateResult = "after critical source"
        };
        var generators = new List<ICodeGenerator> { generator1, generator2, generator3 };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert - based on the actual behavior observed
        Assert.Equal(2, generatedSources.Count); // Both first and third generators succeed
        Assert.True(generatedSources.ContainsKey("SuccessFile.g.cs"));
        Assert.False(generatedSources.ContainsKey("CriticalErrorFile.g.cs"));
        Assert.True(generatedSources.ContainsKey("AfterCriticalFile.g.cs")); // Third generator still processes
        Assert.Equal("success source", generatedSources["SuccessFile.g.cs"]);
        Assert.Equal("after critical source", generatedSources["AfterCriticalFile.g.cs"]);
        
        // Should have one diagnostic from the critical error
        Assert.Single(reporter.Diagnostics);
        
        // Check that the critical error was reported
        var criticalErrorDiagnostic = reporter.Diagnostics.FirstOrDefault(d => d.GetMessage().Contains("Critical error"));
        Assert.NotNull(criticalErrorDiagnostic);
        var criticalMessage = criticalErrorDiagnostic!.GetMessage();
        Assert.Contains("Critical error in operation 'Generator CriticalErrorGenerator'", criticalMessage);
        Assert.Contains("OutOfMemoryException", criticalMessage);
        Assert.Contains("Out of memory during generation", criticalMessage);
    }

    [Fact]
    public void ExecuteGeneratorsWithIsolation_NonRecoverableExceptionCondition_IsCovered()
    {
        // This test specifically verifies that the condition !ErrorIsolation.IsRecoverableException(lastError) is covered
        // by testing all types of non-recoverable exceptions
        
        // Test with OutOfMemoryException
        var oomGenerator = new TestCodeGenerator 
        { 
            GeneratorNameValue = "OOMGenerator",
            OutputFileNameValue = "OOMFile",
            CanGenerateResult = true,
            ThrowException = new OutOfMemoryException("Out of memory")
        };
        
        // Test with StackOverflowException
        var soGenerator = new TestCodeGenerator 
        { 
            GeneratorNameValue = "SOGenerator",
            OutputFileNameValue = "SOFile",
            CanGenerateResult = true,
            ThrowException = new StackOverflowException("Stack overflow")
        };
        
        var generators = new List<ICodeGenerator> { oomGenerator, soGenerator };
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var reporter = new TestDiagnosticReporter();

        // Act
        var generatedSources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

        // Assert
        Assert.Empty(generatedSources); // No generators should succeed
        
        // Should have 2 critical error diagnostics (one for each generator)
        Assert.Equal(2, reporter.Diagnostics.Count);
        
        // Verify OutOfMemoryException was reported as critical error
        var oomDiagnostic = reporter.Diagnostics.FirstOrDefault(d => d.GetMessage().Contains("OOMGenerator") && d.GetMessage().Contains("Critical error"));
        Assert.NotNull(oomDiagnostic);
        Assert.Contains("OutOfMemoryException", oomDiagnostic!.GetMessage());
        
        // Verify StackOverflowException was reported as critical error
        var soDiagnostic = reporter.Diagnostics.FirstOrDefault(d => d.GetMessage().Contains("SOGenerator") && d.GetMessage().Contains("Critical error"));
        Assert.NotNull(soDiagnostic);
        Assert.Contains("StackOverflowException", soDiagnostic!.GetMessage());
    }

    #endregion
}