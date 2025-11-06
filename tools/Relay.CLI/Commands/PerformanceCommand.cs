using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Relay.CLI.Commands.Models.Performance;
using Spectre.Console;
using System.CommandLine;
using System.Text;

namespace Relay.CLI.Commands;

public static class PerformanceCommand
{
    public static Command Create()
    {
        var command = new Command("performance", "Performance analysis and recommendations");

        var pathOption = new Option<string>("--path", () => ".", "Project path to analyze");
        var reportOption = new Option<bool>("--report", () => true, "Generate performance report");
        var detailedOption = new Option<bool>("--detailed", () => false, "Show detailed analysis");
        var outputOption = new Option<string?>("--output", "Output file for the report");

        command.AddOption(pathOption);
        command.AddOption(reportOption);
        command.AddOption(detailedOption);
        command.AddOption(outputOption);

        command.SetHandler(async (path, report, detailed, output) =>
        {
            await ExecutePerformance(path, report, detailed, output);
        }, pathOption, reportOption, detailedOption, outputOption);

        return command;
    }

    internal static async Task ExecutePerformance(string projectPath, bool generateReport, bool detailed, string? outputFile)
    {
        var rule = new Rule("[cyan]⚡ Performance Analysis[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var analysis = new PerformanceAnalysis();

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Analyzing performance characteristics...", async ctx =>
                {
                    ctx.Status("Scanning project files...");
                    await AnalyzeProjectStructure(projectPath, analysis);
                    await Task.Delay(200);

                    ctx.Status("Analyzing async patterns...");
                    await AnalyzeAsyncPatterns(projectPath, analysis);
                    await Task.Delay(200);

                    ctx.Status("Checking memory patterns...");
                    await AnalyzeMemoryPatterns(projectPath, analysis);
                    await Task.Delay(200);

                    ctx.Status("Evaluating handler performance...");
                    await AnalyzeHandlerPerformance(projectPath, analysis);
                    await Task.Delay(200);

                    ctx.Status("Generating recommendations...");
                    await GenerateRecommendations(analysis);
                    await Task.Delay(200);
                });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("interactive functions concurrently"))
        {
            // Fallback for test environments where interactive features are disabled
            AnsiConsole.MarkupLine("[dim]Scanning project files...[/]");
            await AnalyzeProjectStructure(projectPath, analysis);

            AnsiConsole.MarkupLine("[dim]Analyzing async patterns...[/]");
            await AnalyzeAsyncPatterns(projectPath, analysis);

            AnsiConsole.MarkupLine("[dim]Checking memory patterns...[/]");
            await AnalyzeMemoryPatterns(projectPath, analysis);

            AnsiConsole.MarkupLine("[dim]Evaluating handler performance...[/]");
            await AnalyzeHandlerPerformance(projectPath, analysis);

            AnsiConsole.MarkupLine("[dim]Generating recommendations...[/]");
            await GenerateRecommendations(analysis);
        }

        // Display results
        DisplayPerformanceAnalysis(analysis, detailed);

        // Generate report if requested
        if (generateReport)
        {
            var reportPath = outputFile ?? Path.Combine(projectPath, "performance-report.md");
            await GeneratePerformanceReport(analysis, reportPath);
            AnsiConsole.MarkupLine($"[green]📊 Performance report saved: {reportPath}[/]");
        }
    }

    internal static async Task AnalyzeProjectStructure(string path, PerformanceAnalysis analysis)
    {
        var projectFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        analysis.ProjectCount = projectFiles.Count;

        foreach (var projFile in projectFiles)
        {
            var content = await File.ReadAllTextAsync(projFile);
            
            if (content.Contains("Relay.Core") || content.Contains("Relay\""))
            {
                analysis.HasRelay = true;
            }

            // Check for performance settings
            if (content.Contains("<TieredPGO>true</TieredPGO>"))
            {
                analysis.HasPGO = true;
            }

            if (content.Contains("<Optimize>true</Optimize>"))
            {
                analysis.HasOptimizations = true;
            }

            // Check target framework
            if (content.Contains("net8.0") || content.Contains("net9.0"))
            {
                analysis.ModernFramework = true;
            }
        }
    }

    internal static async Task AnalyzeAsyncPatterns(string path, PerformanceAnalysis analysis)
    {
        var csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .Take(100) // Limit for performance
            .ToList();

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = await tree.GetRootAsync();

            // Count async methods
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            
            foreach (var method in methods)
            {
                var modifiers = method.Modifiers.ToString();
                
                if (modifiers.Contains("async"))
                {
                    analysis.AsyncMethodCount++;
                    
                    var returnType = method.ReturnType.ToString();
                    
                    if (returnType.Contains("ValueTask"))
                    {
                        analysis.ValueTaskCount++;
                    }
                    else if (returnType.Contains("Task"))
                    {
                        analysis.TaskCount++;
                    }

                    // Check for ConfigureAwait
                    var methodBody = method.Body?.ToString() ?? "";
                    if (methodBody.Contains("ConfigureAwait(false)"))
                    {
                        analysis.ConfigureAwaitCount++;
                    }
                }

                // Check for CancellationToken
                if (method.ParameterList.Parameters.Any(p => p.Type?.ToString().Contains("CancellationToken") ?? false))
                {
                    analysis.CancellationTokenCount++;
                }
            }
        }
    }

    internal static async Task AnalyzeMemoryPatterns(string path, PerformanceAnalysis analysis)
    {
        var csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .Take(100)
            .ToList();

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            
            // Check for record usage (immutable, efficient)
            var recordMatches = System.Text.RegularExpressions.Regex.Matches(content, @"public\s+record\s+\w+");
            analysis.RecordCount += recordMatches.Count;

            // Check for struct usage
            var structMatches = System.Text.RegularExpressions.Regex.Matches(content, @"public\s+struct\s+\w+");
            analysis.StructCount += structMatches.Count;

            // Check for LINQ usage (potential allocations)
            if (content.Contains(".Select(") || content.Contains(".Where("))
            {
                analysis.LinqUsageCount++;
            }

            // Check for StringBuilder (good for string manipulation)
            if (content.Contains("StringBuilder"))
            {
                analysis.StringBuilderCount++;
            }

            // Check for string concatenation in loops (bad practice)
            if (System.Text.RegularExpressions.Regex.IsMatch(content, @"for\s*\([^)]*\)\s*\{[^}]*\+\s*=\s*[^;]*;") ||
                System.Text.RegularExpressions.Regex.IsMatch(content, @"for\s*\([^)]*\)\s*\{[^}]*=\s*[^+]*\+[^;]*;"))
            {
                analysis.StringConcatInLoopCount++;
            }
        }
    }

    internal static async Task AnalyzeHandlerPerformance(string path, PerformanceAnalysis analysis)
    {
        var csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = await tree.GetRootAsync();

            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                var baseTypes = classDecl.BaseList?.Types;
                if (baseTypes != null)
                {
                    bool isHandler = false;
                    foreach (var baseType in baseTypes)
                    {
                        var typeName = baseType.Type.ToString();
                        if (typeName.Contains("IRequestHandler") || typeName.Contains("INotificationHandler"))
                        {
                            isHandler = true;
                            break;
                        }
                    }

                    if (isHandler)
                    {
                        analysis.HandlerCount++;

                        // Check for [Handle] attribute
                        var attributes = classDecl.AttributeLists.SelectMany(al => al.Attributes);
                        if (attributes.Any(a => a.Name.ToString() == "Handle"))
                        {
                            analysis.OptimizedHandlerCount++;
                        }

                        // Check for caching
                        var classBody = classDecl.ToString();
                        if (classBody.Contains("ICache") || classBody.Contains("IMemoryCache") || classBody.Contains("ICachable"))
                        {
                            analysis.CachedHandlerCount++;
                        }
                    }
                }
            }
        }

        await Task.CompletedTask;
    }

    internal static async Task GenerateRecommendations(PerformanceAnalysis analysis)
    {
        // Generate performance score
        int score = 100;

        if (!analysis.HasRelay) score -= 20;
        if (!analysis.ModernFramework) score -= 10;
        if (!analysis.HasPGO) score -= 5;
        if (!analysis.HasOptimizations) score -= 5;
        
        if (analysis.TaskCount > analysis.ValueTaskCount) score -= 10;
        if (analysis.StringConcatInLoopCount > 0) score -= 15;
        if (analysis.CancellationTokenCount < analysis.AsyncMethodCount * 0.5) score -= 10;
        if (analysis.OptimizedHandlerCount < analysis.HandlerCount * 0.5) score -= 10;

        analysis.PerformanceScore = Math.Max(0, score);

        // Add recommendations
        if (!analysis.HasPGO)
        {
            analysis.Recommendations.Add(new PerformanceRecommendation
            {
                Category = "Build Configuration",
                Priority = "High",
                Title = "Enable Profile-Guided Optimization (PGO)",
                Description = "Add <TieredPGO>true</TieredPGO> to your .csproj for better runtime performance",
                Impact = "10-20% performance improvement"
            });
        }

        if (analysis.TaskCount > analysis.ValueTaskCount)
        {
            analysis.Recommendations.Add(new PerformanceRecommendation
            {
                Category = "Async Patterns",
                Priority = "Medium",
                Title = "Use ValueTask instead of Task",
                Description = "ValueTask reduces allocations for synchronous code paths",
                Impact = "Reduced memory allocations"
            });
        }

        if (analysis.StringConcatInLoopCount > 0)
        {
            analysis.Recommendations.Add(new PerformanceRecommendation
            {
                Category = "Memory Patterns",
                Priority = "High",
                Title = "Avoid string concatenation in loops",
                Description = "Use StringBuilder for string concatenation in loops",
                Impact = "Significant reduction in memory allocations"
            });
        }

        if (analysis.OptimizedHandlerCount < analysis.HandlerCount)
        {
            analysis.Recommendations.Add(new PerformanceRecommendation
            {
                Category = "Handler Optimization",
                Priority = "Medium",
                Title = "Add [[Handle]] attribute to handlers",
                Description = "Enable source generator optimizations for handlers",
                Impact = "Zero-overhead handler invocation"
            });
        }

        if (analysis.CachedHandlerCount == 0 && analysis.HandlerCount > 0)
        {
            analysis.Recommendations.Add(new PerformanceRecommendation
            {
                Category = "Caching",
                Priority = "Low",
                Title = "Consider adding caching",
                Description = "Add response caching for read-heavy operations",
                Impact = "Reduced database/API calls"
            });
        }

        await Task.CompletedTask;
    }

    internal static void DisplayPerformanceAnalysis(PerformanceAnalysis analysis, bool detailed)
    {
        // Performance Score
        var scoreColor = analysis.PerformanceScore >= 80 ? "green" :
                        analysis.PerformanceScore >= 60 ? "yellow" : "red";

        var scorePanel = new Panel($"[{scoreColor} bold]{analysis.PerformanceScore}/100[/]")
            .Header("[bold]Performance Score[/]")
            .BorderColor(Color.Cyan1);

        AnsiConsole.Write(scorePanel);
        AnsiConsole.WriteLine();

        // Metrics Table
        var metricsTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]")
            .AddColumn("[bold]Status[/]");

        metricsTable.AddRow("Projects", analysis.ProjectCount.ToString(), analysis.ProjectCount > 0 ? "✅" : "❌");
        metricsTable.AddRow("Handlers", analysis.HandlerCount.ToString(), analysis.HandlerCount > 0 ? "✅" : "⚠️");
        metricsTable.AddRow("Optimized Handlers", analysis.OptimizedHandlerCount.ToString(), 
            analysis.OptimizedHandlerCount == analysis.HandlerCount ? "✅" : "⚠️");
        metricsTable.AddRow("ValueTask Usage", $"{analysis.ValueTaskCount}/{analysis.AsyncMethodCount}", 
            analysis.ValueTaskCount >= analysis.TaskCount ? "✅" : "⚠️");
        metricsTable.AddRow("Modern Framework", analysis.ModernFramework ? "Yes" : "No", 
            analysis.ModernFramework ? "✅" : "⚠️");
        metricsTable.AddRow("PGO Enabled", analysis.HasPGO ? "Yes" : "No", 
            analysis.HasPGO ? "✅" : "⚠️");

        if (detailed)
        {
            metricsTable.AddRow("Record Types", analysis.RecordCount.ToString(), "ℹ️");
            metricsTable.AddRow("LINQ Usage", analysis.LinqUsageCount.ToString(), "ℹ️");
            metricsTable.AddRow("CancellationToken", $"{analysis.CancellationTokenCount}/{analysis.AsyncMethodCount}", "ℹ️");
        }

        AnsiConsole.Write(metricsTable);
        AnsiConsole.WriteLine();

        // Recommendations
        if (analysis.Recommendations.Count > 0)
        {
            var recTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Priority[/]")
                .AddColumn("[bold]Category[/]")
                .AddColumn("[bold]Recommendation[/]")
                .AddColumn("[bold]Impact[/]");

            foreach (var recommendation in analysis.Recommendations.OrderBy(r => GetPriorityOrder(r.Priority)))
            {
                var priorityColor = recommendation.Priority switch
                {
                    "High" => "red",
                    "Medium" => "yellow",
                    _ => "blue"
                };

                recTable.AddRow(
                    $"[{priorityColor}]{recommendation.Priority}[/]",
                    recommendation.Category,
                    $"{recommendation.Title}\n[dim]{recommendation.Description}[/]",
                    recommendation.Impact
                );
            }

            var recPanel = new Panel(recTable)
                .Header("[bold yellow]⚡ Performance Recommendations[/]")
                .BorderColor(Color.Yellow);

            AnsiConsole.Write(recPanel);
        }
        else
        {
            AnsiConsole.MarkupLine("[green]✨ Excellent! No performance recommendations at this time.[/]");
        }
    }

    internal static int GetPriorityOrder(string priority) => priority switch
    {
        "High" => 1,
        "Medium" => 2,
        "Low" => 3,
        _ => 4
    };

    internal static async Task GeneratePerformanceReport(PerformanceAnalysis analysis, string outputPath)
    {
        var report = new StringBuilder();
        
        report.AppendLine("# Performance Analysis Report");
        report.AppendLine();
        report.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        
        report.AppendLine($"## Performance Score: {analysis.PerformanceScore}/100");
        report.AppendLine();
        
        report.AppendLine("## Metrics");
        report.AppendLine();
        report.AppendLine("| Metric | Value |");
        report.AppendLine("|--------|-------|");
        report.AppendLine($"| Projects | {analysis.ProjectCount} |");
        report.AppendLine($"| Handlers | {analysis.HandlerCount} |");
        report.AppendLine($"| Optimized Handlers | {analysis.OptimizedHandlerCount} |");
        report.AppendLine($"| Async Methods | {analysis.AsyncMethodCount} |");
        report.AppendLine($"| ValueTask Usage | {analysis.ValueTaskCount} |");
        report.AppendLine($"| Task Usage | {analysis.TaskCount} |");
        report.AppendLine($"| Modern Framework | {analysis.ModernFramework} |");
        report.AppendLine($"| PGO Enabled | {analysis.HasPGO} |");
        report.AppendLine();

        if (analysis.Recommendations.Count > 0)
        {
            report.AppendLine("## Recommendations");
            report.AppendLine();
            
            foreach (var recommendation in analysis.Recommendations.OrderBy(r => GetPriorityOrder(r.Priority)))
            {
                report.AppendLine($"### {recommendation.Title}");
                report.AppendLine();
                report.AppendLine($"**Priority:** {recommendation.Priority}");
                report.AppendLine($"**Category:** {recommendation.Category}");
                report.AppendLine($"**Description:** {recommendation.Description}");
                report.AppendLine($"**Impact:** {recommendation.Impact}");
                report.AppendLine();
            }
        }

        report.AppendLine("---");
        report.AppendLine("*Generated by Relay CLI*");

        await File.WriteAllTextAsync(outputPath, report.ToString());
    }
}
