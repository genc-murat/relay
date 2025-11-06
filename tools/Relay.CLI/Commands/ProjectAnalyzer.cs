using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Relay.CLI.Commands.Models;
using Relay.CLI.Commands.Models.Performance;
using Spectre.Console;

namespace Relay.CLI.Commands;

internal class ProjectAnalyzer
{
    public async Task<ProjectAnalysis> AnalyzeProject(string projectPath, string depth, bool includeTests, ProgressContext? ctx = null, ProgressTask? overallTask = null)
    {
        var analysis = new ProjectAnalysis
        {
            ProjectPath = Path.GetFullPath(projectPath),
            AnalysisDepth = depth,
            IncludeTests = includeTests,
            Timestamp = DateTime.UtcNow
        };

        await DiscoverProjectFiles(analysis, ctx, overallTask);
        overallTask?.Increment(1);

        await AnalyzeHandlers(analysis, ctx, overallTask);
        overallTask?.Increment(1);

        await AnalyzeRequests(analysis, ctx, overallTask);
        overallTask?.Increment(1);

        await CheckPerformanceOpportunities(analysis, ctx, overallTask);
        overallTask?.Increment(1);

        await CheckReliabilityPatterns(analysis, ctx, overallTask);
        overallTask?.Increment(1);

        await AnalyzeDependencies(analysis, ctx, overallTask);
        overallTask?.Increment(1);

        await GenerateRecommendations(analysis, ctx, overallTask);
        overallTask?.Increment(1);

        return analysis;
    }

    internal async Task DiscoverProjectFiles(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
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

    internal async Task AnalyzeHandlers(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
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
                    if (AnalysisHelpers.IsHandler(classDecl, content))
                    {
                        var handler = new HandlerInfo
                        {
                            Name = classDecl.Identifier.ValueText,
                            FilePath = file,
                            IsAsync = AnalysisHelpers.HasAsyncMethods(classDecl),
                            HasDependencies = AnalysisHelpers.HasConstructorDependencies(classDecl),
                            UsesValueTask = AnalysisHelpers.UsesValueTask(classDecl, content),
                            HasCancellationToken = AnalysisHelpers.UsesCancellationToken(classDecl, content),
                            HasLogging = AnalysisHelpers.HasLogging(classDecl, content),
                            HasValidation = AnalysisHelpers.HasValidation(classDecl, content),
                            LineCount = AnalysisHelpers.GetMethodLineCount(classDecl)
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

        await Task.CompletedTask;
    }

    internal async Task AnalyzeRequests(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
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
                    if (AnalysisHelpers.IsRequest(typeDecl, content))
                    {
                        var request = new RequestInfo
                        {
                            Name = typeDecl.Identifier.ValueText,
                            FilePath = file,
                            IsRecord = typeDecl is RecordDeclarationSyntax,
                            HasResponse = AnalysisHelpers.HasResponseType(typeDecl, content),
                            HasValidation = AnalysisHelpers.HasValidationAttributes(typeDecl, content),
                            ParameterCount = AnalysisHelpers.GetParameterCount(typeDecl),
                            HasCaching = AnalysisHelpers.HasCachingAttributes(typeDecl, content),
                            HasAuthorization = AnalysisHelpers.HasAuthorizationAttributes(typeDecl, content)
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

        await Task.CompletedTask;
    }

    internal async Task CheckPerformanceOpportunities(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
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

    internal async Task CheckReliabilityPatterns(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
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

    internal async Task AnalyzeDependencies(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
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

                if (content.Contains("StackExchange.Redis") || content.Contains("Microsoft.Extensions.Caching"))
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

        await Task.CompletedTask;
    }

    internal async Task GenerateRecommendations(ProjectAnalysis analysis, ProgressContext? ctx, ProgressTask? overallTask)
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
}