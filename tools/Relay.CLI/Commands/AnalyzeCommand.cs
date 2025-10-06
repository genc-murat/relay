using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;

namespace Relay.CLI.Commands;

public static class AnalyzeCommand
{
    public static Command Create()
    {
        var command = new Command("analyze", "Analyze your project for performance optimization opportunities");

        var pathOption = new Option<string>("--path", () => ".", "Project path to analyze");
        var outputOption = new Option<string>("--output", "Output file for analysis report");
        var formatOption = new Option<string>("--format", () => "console", "Output format (console, json, html, markdown)");
        var depthOption = new Option<string>("--depth", () => "full", "Analysis depth (quick, standard, full, deep)");
        var includeTestsOption = new Option<bool>("--include-tests", () => false, "Include test projects in analysis");

        command.AddOption(pathOption);
        command.AddOption(outputOption);
        command.AddOption(formatOption);
        command.AddOption(depthOption);
        command.AddOption(includeTestsOption);

        command.SetHandler(ExecuteAnalyze, pathOption, outputOption, formatOption, depthOption, includeTestsOption);

        return command;
    }

    [RequiresUnreferencedCode("Calls Relay.CLI.Commands.AnalyzeCommand.SaveAnalysisResults(ProjectAnalysis, String, String)")]
    [RequiresDynamicCode("Calls Relay.CLI.Commands.AnalyzeCommand.SaveAnalysisResults(ProjectAnalysis, String, String)")]
    private static async Task<int> ExecuteAnalyze(string projectPath, string? outputPath, string format, string depth, bool includeTests)
    {
        AnsiConsole.MarkupLine($"[cyan]🔍 Analyzing project at: {projectPath}[/]");
        AnsiConsole.WriteLine();

        var analysis = new ProjectAnalysis
        {
            ProjectPath = Path.GetFullPath(projectPath),
            AnalysisDepth = depth,
            IncludeTests = includeTests,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var overallTask = ctx?.AddTask("[cyan]Analyzing project[/]", maxValue: 7);

                    // Discover project files
                    await DiscoverProjectFiles(analysis, ctx, overallTask);
                    overallTask?.Increment(1);

                    // Analyze handlers
                    await AnalyzeHandlers(analysis, ctx, overallTask);
                    overallTask?.Increment(1);

                    // Analyze requests
                    await AnalyzeRequests(analysis, ctx, overallTask);
                    overallTask?.Increment(1);

                    // Check performance opportunities
                    await CheckPerformanceOpportunities(analysis, ctx, overallTask);
                    overallTask?.Increment(1);

                    // Check reliability patterns
                    await CheckReliabilityPatterns(analysis, ctx, overallTask);
                    overallTask?.Increment(1);

                    // Analyze dependencies
                    await AnalyzeDependencies(analysis, ctx, overallTask);
                    overallTask?.Increment(1);

                    // Generate recommendations
                    await GenerateRecommendations(analysis, ctx, overallTask);
                    overallTask?.Increment(1);

                    if (overallTask != null) overallTask.Value = overallTask.MaxValue;
                });
        }
        catch (Exception ex)
        {
            // Log the progress error but continue execution
            // This can happen in test environments where console features are not available
            Console.WriteLine($"Warning: Could not show progress in console: {ex.Message}");
            
            // Run analysis without progress UI in test environments
            await DiscoverProjectFiles(analysis, null!, null!);
            await AnalyzeHandlers(analysis, null!, null!);
            await AnalyzeRequests(analysis, null!, null!);
            await CheckPerformanceOpportunities(analysis, null!, null!);
            await CheckReliabilityPatterns(analysis, null!, null!);
            await AnalyzeDependencies(analysis, null!, null!);
            await GenerateRecommendations(analysis, null!, null!);
        }

        try
        {
            // Display results
            DisplayAnalysisResults(analysis, format);
        }
        catch (Exception ex)
        {
            // Log the display error but continue execution
            Console.WriteLine($"Warning: Could not display results in console: {ex.Message}");
        }

        // Save results if requested
        if (!string.IsNullOrEmpty(outputPath))
        {
            await SaveAnalysisResults(analysis, outputPath, format);
        }
        
        return 0;
    }

    private static async Task DiscoverProjectFiles(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
    {
        var discoveryTask = ctx?.AddTask("[green]Discovering project files[/]");

        try
        {
            // Find .csproj files
            var csprojFiles = Directory.GetFiles(analysis.ProjectPath, "*.csproj", SearchOption.AllDirectories);
            analysis.ProjectFiles.AddRange(csprojFiles);

            // Find .cs files
            var csFiles = Directory.GetFiles(analysis.ProjectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .Where(f => analysis.IncludeTests || !f.Contains("Test"))
                .ToList();

            analysis.SourceFiles.AddRange(csFiles);
        }
        catch (UnauthorizedAccessException)
        {
            // Handle cases where the directory has restricted access
            AnsiConsole.MarkupLine("[yellow]⚠️ Warning: Insufficient permissions to access some directories[/]");
        }
        catch (DirectoryNotFoundException)
        {
            // Handle cases where the directory doesn't exist
            AnsiConsole.MarkupLine("[red]❌ Error: Project directory does not exist[/]");
            throw; // Re-throw to indicate failure
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error discovering project files: {ex.Message}[/]");
            throw; // Re-throw to indicate failure
        }

        if (discoveryTask != null) discoveryTask.Value = discoveryTask.MaxValue;
        AnsiConsole.MarkupLine($"[dim]Found {analysis.ProjectFiles.Count} project(s) and {analysis.SourceFiles.Count} source file(s)[/]");

        await Task.CompletedTask;
    }

    private static async Task AnalyzeHandlers(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
    {
        var handlerTask = ctx?.AddTask("[green]Analyzing handlers[/]");
        var handlerCount = 0;

        foreach (var file in analysis.SourceFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(content);
                var root = tree.GetCompilationUnitRoot();

                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classes)
                {
                    if (IsHandler(classDecl, content))
                    {
                        var handler = new HandlerInfo
                        {
                            Name = classDecl.Identifier.ValueText,
                            FilePath = file,
                            IsAsync = HasAsyncMethods(classDecl),
                            HasDependencies = HasConstructorDependencies(classDecl),
                            UsesValueTask = UsesValueTask(classDecl, content),
                            HasCancellationToken = UsesCancellationToken(classDecl, content),
                            HasLogging = HasLogging(classDecl, content),
                            HasValidation = HasValidation(classDecl, content),
                            LineCount = GetMethodLineCount(classDecl)
                        };

                        analysis.Handlers.Add(handler);
                        handlerCount++;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Warning: File not found, skipping: {file}[/]");
                continue; // Continue with next file instead of failing the entire analysis
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Warning: Access denied, skipping: {file}[/]");
                continue;
            }
            catch (IOException ex)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Warning: IO error reading {file}, skipping: {ex.Message}[/]");
                continue;
            }
        }

        if (handlerTask != null) handlerTask.Value = handlerTask.MaxValue;
        AnsiConsole.MarkupLine($"[dim]Analyzed {handlerCount} handler(s)[/]");
    }

    private static async Task AnalyzeRequests(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
    {
        var requestTask = ctx?.AddTask("[green]Analyzing requests[/]");
        var requestCount = 0;

        foreach (var file in analysis.SourceFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(content);
                var root = tree.GetCompilationUnitRoot();

                var records = root.DescendantNodes().OfType<RecordDeclarationSyntax>();
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var typeDecl in records.Cast<TypeDeclarationSyntax>().Concat(classes))
                {
                    if (IsRequest(typeDecl, content))
                    {
                        var request = new RequestInfo
                        {
                            Name = typeDecl.Identifier.ValueText,
                            FilePath = file,
                            IsRecord = typeDecl is RecordDeclarationSyntax,
                            HasResponse = HasResponseType(typeDecl, content),
                            HasValidation = HasValidationAttributes(typeDecl, content),
                            ParameterCount = GetParameterCount(typeDecl),
                            HasCaching = HasCachingAttributes(typeDecl, content),
                            HasAuthorization = HasAuthorizationAttributes(typeDecl, content)
                        };

                        analysis.Requests.Add(request);
                        requestCount++;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Warning: File not found, skipping: {file}[/]");
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Warning: Access denied, skipping: {file}[/]");
                continue;
            }
            catch (IOException ex)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Warning: IO error reading {file}, skipping: {ex.Message}[/]");
                continue;
            }
        }

        if (requestTask != null) requestTask.Value = requestTask.MaxValue;
        AnsiConsole.MarkupLine($"[dim]Analyzed {requestCount} request(s)[/]");
    }

    private static async Task CheckPerformanceOpportunities(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
    {
        var perfTask = ctx?.AddTask("[yellow]Checking performance opportunities[/]");

        // Check for Task vs ValueTask usage
        var taskHandlers = analysis.Handlers.Count(h => !h.UsesValueTask);
        if (taskHandlers > 0)
        {
            analysis.PerformanceIssues.Add(new PerformanceIssue
            {
                Type = "Task Usage",
                Severity = "Medium",
                Count = taskHandlers,
                Description = $"{taskHandlers} handler(s) use Task instead of ValueTask",
                Recommendation = "Switch to ValueTask<T> for better performance",
                PotentialImprovement = "5-15% faster execution"
            });
        }

        // Check for missing cancellation tokens
        var noCancellationHandlers = analysis.Handlers.Count(h => !h.HasCancellationToken);
        if (noCancellationHandlers > 0)
        {
            analysis.PerformanceIssues.Add(new PerformanceIssue
            {
                Type = "Cancellation Support",
                Severity = "Low",
                Count = noCancellationHandlers,
                Description = $"{noCancellationHandlers} handler(s) don't use CancellationToken",
                Recommendation = "Add CancellationToken parameters for better responsiveness",
                PotentialImprovement = "Better resource management"
            });
        }

        // Check for potential caching opportunities
        var queryHandlers = analysis.Handlers.Where(h => h.Name.Contains("Query") || h.Name.Contains("Get")).Count();
        var cachedRequests = analysis.Requests.Count(r => r.HasCaching);
        if (queryHandlers > cachedRequests)
        {
            analysis.PerformanceIssues.Add(new PerformanceIssue
            {
                Type = "Caching Opportunity",
                Severity = "Medium",
                Count = queryHandlers - cachedRequests,
                Description = $"{queryHandlers - cachedRequests} query handler(s) could benefit from caching",
                Recommendation = "Add caching attributes to read-heavy operations",
                PotentialImprovement = "50-90% reduction in response time for cached data"
            });
        }

        // Check for large handlers (potential for optimization)
        var largeHandlers = analysis.Handlers.Where(h => h.LineCount > 100).Count();
        if (largeHandlers > 0)
        {
            analysis.PerformanceIssues.Add(new PerformanceIssue
            {
                Type = "Handler Complexity",
                Severity = "Low",
                Count = largeHandlers,
                Description = $"{largeHandlers} handler(s) are very large (>100 lines)",
                Recommendation = "Consider breaking down large handlers or optimizing logic",
                PotentialImprovement = "Better maintainability and potential performance gains"
            });
        }

        if (perfTask != null) perfTask.Value = perfTask.MaxValue;
        AnsiConsole.MarkupLine($"[dim]Found {analysis.PerformanceIssues.Count} performance opportunity/opportunities[/]");

        await Task.CompletedTask;
    }

    private static async Task CheckReliabilityPatterns(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
    {
        var reliabilityTask = ctx?.AddTask("[yellow]Checking reliability patterns[/]");

        // Check for logging
        var noLoggingHandlers = analysis.Handlers.Count(h => !h.HasLogging);
        if (noLoggingHandlers > 0)
        {
            analysis.ReliabilityIssues.Add(new ReliabilityIssue
            {
                Type = "Logging",
                Severity = "Medium",
                Count = noLoggingHandlers,
                Description = $"{noLoggingHandlers} handler(s) don't have logging",
                Recommendation = "Add logging for better observability and debugging",
                Impact = "Improved troubleshooting and monitoring"
            });
        }

        // Check for validation
        var noValidationRequests = analysis.Requests.Count(r => !r.HasValidation);
        if (noValidationRequests > 0)
        {
            analysis.ReliabilityIssues.Add(new ReliabilityIssue
            {
                Type = "Validation",
                Severity = "High",
                Count = noValidationRequests,
                Description = $"{noValidationRequests} request(s) don't have validation attributes",
                Recommendation = "Add validation attributes to prevent invalid data processing",
                Impact = "Better data integrity and error prevention"
            });
        }

        // Check for authorization
        var noAuthRequests = analysis.Requests.Count(r => !r.HasAuthorization);
        if (noAuthRequests > 0 && analysis.Requests.Count > 0)
        {
            analysis.ReliabilityIssues.Add(new ReliabilityIssue
            {
                Type = "Authorization",
                Severity = "High",
                Count = noAuthRequests,
                Description = $"{noAuthRequests} request(s) don't have authorization attributes",
                Recommendation = "Add authorization attributes to secure endpoints",
                Impact = "Improved security and access control"
            });
        }

        if (reliabilityTask != null) reliabilityTask.Value = reliabilityTask.MaxValue;
        AnsiConsole.MarkupLine($"[dim]Found {analysis.ReliabilityIssues.Count} reliability issue(s)[/]");

        await Task.CompletedTask;
    }

    private static async Task AnalyzeDependencies(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
    {
        var depTask = ctx?.AddTask("[yellow]Analyzing dependencies[/]");

        foreach (var projectFile in analysis.ProjectFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(projectFile);

                // Check for Relay references
                if (content.Contains("Relay.Core"))
                {
                    analysis.HasRelayCore = true;
                }

                // Check for other mediator frameworks
                if (content.Contains("MediatR"))
                {
                    analysis.HasMediatR = true;
                }

                // Check for relevant packages
                if (content.Contains("Microsoft.Extensions.Logging"))
                {
                    analysis.HasLogging = true;
                }

                if (content.Contains("FluentValidation") || content.Contains("DataAnnotations"))
                {
                    analysis.HasValidation = true;
                }

                if (content.Contains("StackExchangeRedis") || content.Contains("Microsoft.Extensions.Caching"))
                {
                    analysis.HasCaching = true;
                }
            }
            catch (FileNotFoundException)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Warning: Project file not found, skipping: {projectFile}[/]");
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Warning: Access denied to project file, skipping: {projectFile}[/]");
                continue;
            }
            catch (IOException ex)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Warning: IO error reading project file {projectFile}, skipping: {ex.Message}[/]");
                continue;
            }
        }

        if (depTask != null) depTask.Value = depTask.MaxValue;
        AnsiConsole.MarkupLine($"[dim]Analyzed project dependencies[/]");
    }

    private static async Task GenerateRecommendations(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
    {
        var recTask = ctx?.AddTask("[green]Generating recommendations[/]");

        // Performance recommendations
        if (analysis.PerformanceIssues.Any())
        {
            analysis.Recommendations.Add(new Recommendation
            {
                Category = "Performance",
                Priority = "High",
                Title = "Optimize Handler Performance",
                Description = "Several performance optimization opportunities found",
                Actions = analysis.PerformanceIssues.Select(i => i.Recommendation).ToList(),
                EstimatedImpact = "20-50% performance improvement"
            });
        }

        // Reliability recommendations
        if (analysis.ReliabilityIssues.Any())
        {
            analysis.Recommendations.Add(new Recommendation
            {
                Category = "Reliability",
                Priority = "High",
                Title = "Improve Reliability Patterns",
                Description = "Add missing reliability patterns for production readiness",
                Actions = analysis.ReliabilityIssues.Select(i => i.Recommendation).ToList(),
                EstimatedImpact = "Better error handling and monitoring"
            });
        }

        // Framework recommendations
        if (!analysis.HasRelayCore)
        {
            analysis.Recommendations.Add(new Recommendation
            {
                Category = "Framework",
                Priority = "Medium",
                Title = "Adopt Relay Framework",
                Description = "Consider migrating to Relay for better performance",
                Actions = new List<string>
                {
                    "Install Relay.Core NuGet package",
                    "Migrate handlers to use [Handle] attribute",
                    "Update service registration to use AddRelay()",
                    "Run performance benchmarks to measure improvement"
                },
                EstimatedImpact = "67% faster than MediatR, zero-allocation patterns"
            });
        }

        // Architecture recommendations
        if (analysis.Handlers.Count > 20)
        {
            analysis.Recommendations.Add(new Recommendation
            {
                Category = "Architecture",
                Priority = "Medium",
                Title = "Consider Modular Architecture",
                Description = "Large number of handlers detected",
                Actions = new List<string>
                {
                    "Group related handlers into feature modules",
                    "Use named handlers for different strategies",
                    "Implement proper error boundaries",
                    "Consider CQRS pattern for complex operations"
                },
                EstimatedImpact = "Better maintainability and team scalability"
            });
        }

        if (recTask != null) recTask.Value = recTask.MaxValue;
        AnsiConsole.MarkupLine($"[dim]Generated {analysis.Recommendations.Count} recommendation(s)[/]");

        await Task.CompletedTask;
    }

    private static void DisplayAnalysisResults(ProjectAnalysis analysis, string format)
    {
        // Project overview
        var overviewTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Title("[cyan]📊 Project Overview[/]");

        overviewTable.AddColumn("Metric");
        overviewTable.AddColumn("Count");

        overviewTable.AddRow("Project Files", analysis.ProjectFiles.Count.ToString());
        overviewTable.AddRow("Source Files", analysis.SourceFiles.Count.ToString());
        overviewTable.AddRow("Handlers Found", analysis.Handlers.Count.ToString());
        overviewTable.AddRow("Requests Found", analysis.Requests.Count.ToString());
        overviewTable.AddRow("Performance Issues", analysis.PerformanceIssues.Count.ToString());
        overviewTable.AddRow("Reliability Issues", analysis.ReliabilityIssues.Count.ToString());

        AnsiConsole.Write(overviewTable);
        AnsiConsole.WriteLine();

        // Performance issues
        if (analysis.PerformanceIssues.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚡ Performance Opportunities Found:[/]");
            foreach (var issue in analysis.PerformanceIssues)
            {
                var color = issue.Severity switch
                {
                    "High" => "red",
                    "Medium" => "yellow",
                    _ => "white"
                };

                AnsiConsole.MarkupLine($"[{color}]• {issue.Description}[/]");
                AnsiConsole.MarkupLine($"[dim]  Recommendation: {issue.Recommendation}[/]");
                if (!string.IsNullOrEmpty(issue.PotentialImprovement))
                {
                    AnsiConsole.MarkupLine($"[dim]  Impact: {issue.PotentialImprovement}[/]");
                }
                AnsiConsole.WriteLine();
            }
        }

        // Reliability issues
        if (analysis.ReliabilityIssues.Any())
        {
            AnsiConsole.MarkupLine("[yellow]🛡️ Reliability Improvements:[/]");
            foreach (var issue in analysis.ReliabilityIssues)
            {
                var color = issue.Severity switch
                {
                    "High" => "red",
                    "Medium" => "yellow",
                    _ => "white"
                };

                AnsiConsole.MarkupLine($"[{color}]• {issue.Description}[/]");
                AnsiConsole.MarkupLine($"[dim]  Recommendation: {issue.Recommendation}[/]");
                AnsiConsole.WriteLine();
            }
        }

        // Recommendations
        if (analysis.Recommendations.Any())
        {
            AnsiConsole.MarkupLine("[green]🎯 Action Plan:[/]");
            var priorityOrder = new[] { "High", "Medium", "Low" };

            foreach (var priority in priorityOrder)
            {
                var recs = analysis.Recommendations.Where(r => r.Priority == priority).ToList();
                if (!recs.Any()) continue;

                var color = priority switch
                {
                    "High" => "red",
                    "Medium" => "yellow",
                    _ => "green"
                };

                AnsiConsole.MarkupLine($"[{color}]{priority} Priority:[/]");
                foreach (var rec in recs)
                {
                    AnsiConsole.MarkupLine($"[{color}]  📋 {rec.Title}[/]");
                    AnsiConsole.MarkupLine($"[dim]     {rec.Description}[/]");
                    foreach (var action in rec.Actions.Take(3))
                    {
                        AnsiConsole.MarkupLine($"[dim]     • {action}[/]");
                    }
                    if (rec.Actions.Count > 3)
                    {
                        AnsiConsole.MarkupLine($"[dim]     • ... and {rec.Actions.Count - 3} more action(s)[/]");
                    }
                    AnsiConsole.WriteLine();
                }
            }
        }

        // Overall score
        var score = CalculateOverallScore(analysis);
        var scoreColor = score >= 8 ? "green" : score >= 6 ? "yellow" : "red";
        var emoji = score >= 8 ? "🏆" : score >= 6 ? "👍" : "⚠️";

        AnsiConsole.MarkupLine($"[{scoreColor}]{emoji} Overall Score: {score:F1}/10[/]");

        var assessment = score switch
        {
            >= 9 => "Excellent - Production ready with great performance patterns!",
            >= 8 => "Very Good - Minor optimizations could provide additional benefits",
            >= 7 => "Good - Some improvements recommended for better performance",
            >= 6 => "Fair - Several optimization opportunities available",
            >= 5 => "Needs Improvement - Multiple issues should be addressed",
            _ => "Poor - Significant improvements needed before production use"
        };

        AnsiConsole.MarkupLine($"[dim]{assessment}[/]");
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private static async Task SaveAnalysisResults(ProjectAnalysis analysis, string outputPath, string format)
    {
        var content = format.ToLower() switch
        {
            "json" => System.Text.Json.JsonSerializer.Serialize(analysis, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            "html" => GenerateHtmlAnalysisReport(analysis),
            "markdown" => GenerateMarkdownReport(analysis),
            _ => System.Text.Json.JsonSerializer.Serialize(analysis, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
        };

        try
        {
            await File.WriteAllTextAsync(outputPath, content);
            AnsiConsole.MarkupLine($"[green]✓ Analysis report saved to: {outputPath}[/]");
        }
        catch (UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error: Insufficient permissions to write to {outputPath}[/]");
            throw;
        }
        catch (DirectoryNotFoundException)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error: Directory does not exist: {Path.GetDirectoryName(outputPath)}[/]");
            throw;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error saving analysis report: {ex.Message}[/]");
            throw;
        }
    }

    // Helper methods for analysis
    private static bool IsHandler(ClassDeclarationSyntax classDecl, string content) =>
        classDecl.Identifier.ValueText.EndsWith("Handler") ||
        content.Contains("[Handle]") ||
        classDecl.BaseList?.Types.Any(t => t.ToString().Contains("IRequestHandler")) == true;

    private static bool IsRequest(TypeDeclarationSyntax typeDecl, string content) =>
        typeDecl.Identifier.ValueText.EndsWith("Request") ||
        typeDecl.Identifier.ValueText.EndsWith("Query") ||
        typeDecl.Identifier.ValueText.EndsWith("Command") ||
        typeDecl.BaseList?.Types.Any(t => t.ToString().Contains("IRequest")) == true;

    private static bool HasAsyncMethods(ClassDeclarationSyntax classDecl) =>
        classDecl.Members.OfType<MethodDeclarationSyntax>()
            .Any(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)));

    private static bool HasConstructorDependencies(ClassDeclarationSyntax classDecl) =>
        classDecl.Members.OfType<ConstructorDeclarationSyntax>()
            .Any(c => c.ParameterList.Parameters.Count > 0);

    private static bool UsesValueTask(ClassDeclarationSyntax classDecl, string content) =>
        content.Contains("ValueTask");

    private static bool UsesCancellationToken(ClassDeclarationSyntax classDecl, string content) =>
        content.Contains("CancellationToken");

    private static bool HasLogging(ClassDeclarationSyntax classDecl, string content) =>
        content.Contains("ILogger") || content.Contains("_logger");

    private static bool HasValidation(ClassDeclarationSyntax classDecl, string content) =>
        content.Contains("ValidationAttribute") || content.Contains("[Required]");

    private static int GetMethodLineCount(ClassDeclarationSyntax classDecl) =>
        classDecl.Members.OfType<MethodDeclarationSyntax>()
            .Sum(m => m.GetText().Lines.Count);

    private static bool HasResponseType(TypeDeclarationSyntax typeDecl, string content) =>
        typeDecl.BaseList?.Types.Any(t => t.ToString().Contains("IRequest<")) == true;

    private static bool HasValidationAttributes(TypeDeclarationSyntax typeDecl, string content) =>
        content.Contains("[Required]") || content.Contains("[StringLength]") || content.Contains("ValidationAttribute");

    private static int GetParameterCount(TypeDeclarationSyntax typeDecl) =>
        typeDecl is RecordDeclarationSyntax record ?
            record.ParameterList?.Parameters.Count ?? 0 :
            typeDecl.Members.OfType<PropertyDeclarationSyntax>().Count();

    private static bool HasCachingAttributes(TypeDeclarationSyntax typeDecl, string content) =>
        content.Contains("[Cacheable]") || content.Contains("CacheAttribute");

    private static bool HasAuthorizationAttributes(TypeDeclarationSyntax typeDecl, string content) =>
        content.Contains("[Authorize]") || content.Contains("AuthorizeAttribute");

    private static double CalculateOverallScore(ProjectAnalysis analysis)
    {
        try
        {
            double score = 10.0;

            // Deduct for performance issues
            score -= (analysis.PerformanceIssues?.Count(i => i.Severity == "High") ?? 0) * 2.0;
            score -= (analysis.PerformanceIssues?.Count(i => i.Severity == "Medium") ?? 0) * 1.0;
            score -= (analysis.PerformanceIssues?.Count(i => i.Severity == "Low") ?? 0) * 0.5;

            // Deduct for reliability issues
            score -= (analysis.ReliabilityIssues?.Count(i => i.Severity == "High") ?? 0) * 1.5;
            score -= (analysis.ReliabilityIssues?.Count(i => i.Severity == "Medium") ?? 0) * 0.8;
            score -= (analysis.ReliabilityIssues?.Count(i => i.Severity == "Low") ?? 0) * 0.3;

            // Bonus for good practices
            if (analysis.HasRelayCore) score += 0.5;
            if (analysis.HasLogging) score += 0.3;
            if (analysis.HasValidation) score += 0.3;
            if (analysis.HasCaching) score += 0.2;

            return Math.Max(0, Math.Min(10, score));
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error calculating overall score: {ex.Message}[/]");
            return 0.0; // Return a safe default
        }
    }

    private static string GenerateHtmlAnalysisReport(ProjectAnalysis analysis)
    {
        try
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Relay Project Analysis Report</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; }}
        .container {{ max-width: 1200px; margin: 0 auto; }}
        .section {{ margin: 20px 0; padding: 20px; border-radius: 8px; }}
        .overview {{ background: #f8f9fa; }}
        .performance {{ background: #fff3cd; }}
        .reliability {{ background: #d1ecf1; }}
        .recommendations {{ background: #d4edda; }}
        .score {{ text-align: center; font-size: 2em; margin: 20px 0; }}
        .issue {{ margin: 10px 0; padding: 10px; border-left: 4px solid #ffc107; }}
        .high {{ border-left-color: #dc3545; }}
        .medium {{ border-left-color: #ffc107; }}
        .low {{ border-left-color: #28a745; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🔍 Relay Project Analysis Report</h1>
        <p>Generated: {analysis.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</p>
        
        <div class='score'>
            Overall Score: {CalculateOverallScore(analysis):F1}/10
        </div>
        
        <div class='section overview'>
            <h2>📊 Project Overview</h2>
            <ul>
                <li>Project Files: {analysis.ProjectFiles.Count}</li>
                <li>Source Files: {analysis.SourceFiles.Count}</li>
                <li>Handlers Found: {analysis.Handlers.Count}</li>
                <li>Requests Found: {analysis.Requests.Count}</li>
            </ul>
        </div>
        
        <div class='section performance'>
            <h2>⚡ Performance Issues</h2>
            {string.Join("", analysis.PerformanceIssues.Select(i => $@"
            <div class='issue {i.Severity.ToLower()}'>
                <h4>{i.Description}</h4>
                <p><strong>Recommendation:</strong> {i.Recommendation}</p>
                <p><strong>Impact:</strong> {i.PotentialImprovement}</p>
            </div>"))}
        </div>
        
        <div class='section recommendations'>
            <h2>🎯 Recommendations</h2>
            {string.Join("", analysis.Recommendations.Select(r => $@"
            <div class='issue'>
                <h4>{r.Title}</h4>
                <p>{r.Description}</p>
                <ul>
                    {string.Join("", r.Actions.Select(a => $"<li>{a}</li>"))}
                </ul>
                <p><strong>Estimated Impact:</strong> {r.EstimatedImpact}</p>
            </div>"))}
        </div>
    </div>
</body>
</html>";
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating HTML report: {ex.Message}[/]");
            // Return a minimal HTML to ensure command still succeeds
            return $"<html><body><h1>Relay Project Analysis Report</h1><p>Generated: {analysis.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</p><p>Report generation failed: {ex.Message}</p></body></html>";
        }
    }

    private static string GenerateMarkdownReport(ProjectAnalysis analysis)
    {
        try
        {
            var md = new StringBuilder();
            md.AppendLine("# 🔍 Relay Project Analysis Report");
            md.AppendLine($"Generated: {analysis.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            md.AppendLine();

            md.AppendLine($"## Overall Score: {CalculateOverallScore(analysis):F1}/10");
            md.AppendLine();

            md.AppendLine("## 📊 Project Overview");
            md.AppendLine($"- Project Files: {analysis.ProjectFiles.Count}");
            md.AppendLine($"- Source Files: {analysis.SourceFiles.Count}");
            md.AppendLine($"- Handlers Found: {analysis.Handlers.Count}");
            md.AppendLine($"- Requests Found: {analysis.Requests.Count}");
            md.AppendLine();

            if (analysis.PerformanceIssues.Any())
            {
                md.AppendLine("## ⚡ Performance Issues");
                foreach (var issue in analysis.PerformanceIssues)
                {
                    md.AppendLine($"### {issue.Description}");
                    md.AppendLine($"**Severity:** {issue.Severity}");
                    md.AppendLine($"**Recommendation:** {issue.Recommendation}");
                    md.AppendLine($"**Impact:** {issue.PotentialImprovement}");
                    md.AppendLine();
                }
            }

            if (analysis.Recommendations.Any())
            {
                md.AppendLine("## 🎯 Recommendations");
                foreach (var rec in analysis.Recommendations)
                {
                    md.AppendLine($"### {rec.Title}");
                    md.AppendLine($"**Priority:** {rec.Priority}");
                    md.AppendLine($"**Description:** {rec.Description}");
                    md.AppendLine("**Actions:**");
                    foreach (var action in rec.Actions)
                    {
                        md.AppendLine($"- {action}");
                    }
                    md.AppendLine($"**Estimated Impact:** {rec.EstimatedImpact}");
                    md.AppendLine();
                }
            }

            return md.ToString();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating markdown report: {ex.Message}[/]");
            return $"# Relay Project Analysis Report\nGenerated: {analysis.Timestamp:yyyy-MM-dd HH:mm:ss} UTC\n\nReport generation failed: {ex.Message}";
        }
    }
}


