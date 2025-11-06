using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.CLI.Migration;

/// <summary>
/// Analyzes projects for MediatR usage and generates compatibility report
/// </summary>
public class MediatRAnalyzer
{
    public async Task<AnalysisResult> AnalyzeProjectAsync(string projectPath)
    {
        var result = new AnalysisResult
        {
            ProjectPath = projectPath,
            AnalysisDate = DateTime.UtcNow
        };

        try
        {
            // Find project files
            var projectFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .ToList();

            if (projectFiles.Count == 0)
            {
                result.Issues.Add(new MigrationIssue
                {
                    Severity = IssueSeverity.Error,
                    Message = "No project files found",
                    Code = "NO_PROJECT"
                });
                result.CanMigrate = false;
                return result;
            }

            // Analyze package references
            foreach (var projFile in projectFiles)
            {
                await AnalyzePackageReferences(projFile, result);
            }

            // Analyze code files
            var codeFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\Migrations\\"))
                .ToList();

            foreach (var file in codeFiles)
            {
                await AnalyzeCodeFile(file, result);
            }

            // Validate if migration can proceed
            result.FilesAffected = result.FilesWithMediatR.Count;
            result.CanMigrate = !result.Issues.Any(i => i.Severity == IssueSeverity.Error);

            // Add info about what will be migrated
            if (result.HandlersFound > 0)
            {
                result.Issues.Add(new MigrationIssue
                {
                    Severity = IssueSeverity.Info,
                    Message = $"Found {result.HandlersFound} handler(s) to migrate",
                    Code = "INFO_HANDLERS"
                });
            }

            if (result.RequestsFound > 0)
            {
                result.Issues.Add(new MigrationIssue
                {
                    Severity = IssueSeverity.Info,
                    Message = $"Found {result.RequestsFound} request(s) (compatible with Relay)",
                    Code = "INFO_REQUESTS"
                });
            }
        }
        catch (Exception ex)
        {
            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Error,
                Message = $"Analysis failed: {ex.Message}",
                Code = "ANALYSIS_ERROR"
            });
            result.CanMigrate = false;
        }

        return result;
    }

    private async Task AnalyzePackageReferences(string projectFile, AnalysisResult result)
    {
        var content = await File.ReadAllTextAsync(projectFile);

        // Check for MediatR packages
        var mediatrPackages = new[]
        {
            "MediatR",
            "MediatR.Extensions.Microsoft.DependencyInjection",
            "MediatR.Contracts"
        };

        foreach (var package in mediatrPackages)
        {
            if (content.Contains($"<PackageReference Include=\"{package}\""))
            {
                // Extract version (simple regex)
                var versionMatch = System.Text.RegularExpressions.Regex.Match(
                    content, 
                    $@"<PackageReference Include=""{package}"" Version=""([^""]+)"""
                );

                var version = versionMatch.Success ? versionMatch.Groups[1].Value : "unknown";

                result.PackageReferences.Add(new PackageReference
                {
                    Name = package,
                    CurrentVersion = version,
                    ProjectFile = projectFile
                });
            }
        }

        // Check for incompatible packages
        var incompatiblePackages = new[] { "AutoMapper.Extensions.Microsoft.DependencyInjection" };
        foreach (var package in incompatiblePackages)
        {
            if (content.Contains($"<PackageReference Include=\"{package}\""))
            {
                result.Issues.Add(new MigrationIssue
                {
                    Severity = IssueSeverity.Warning,
                    Message = $"Package {package} may need manual review after migration",
                    Code = "PACKAGE_WARNING",
                    FilePath = projectFile
                });
            }
        }
    }

    internal async Task AnalyzeCodeFile(string filePath, AnalysisResult result)
    {
        var content = await File.ReadAllTextAsync(filePath);

        // Quick check if file uses MediatR
        if (!content.Contains("MediatR") && 
            !content.Contains("IRequest") && 
            !content.Contains("INotification"))
        {
            return;
        }

        result.FilesWithMediatR.Add(filePath);

        try
        {
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = await tree.GetRootAsync();

            // Analyze using directives
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            foreach (var usingDir in usingDirectives)
            {
                var name = usingDir.Name?.ToString() ?? "";
                if (name == "MediatR" || name.StartsWith("MediatR."))
                {
                    // Will need to change to Relay.Core
                }
            }

            // Analyze classes
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDecl in classes)
            {
                AnalyzeClass(classDecl, filePath, result);
            }

            // Analyze records
            var records = root.DescendantNodes().OfType<RecordDeclarationSyntax>();
            foreach (var recordDecl in records)
            {
                AnalyzeRecord(recordDecl, filePath, result);
            }

            // Analyze advanced patterns
            AnalyzeAdvancedPatterns(root, filePath, result);
        }
        catch (Exception ex)
        {
            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Warning,
                Message = $"Could not fully analyze {Path.GetFileName(filePath)}: {ex.Message}",
                Code = "PARSE_WARNING",
                FilePath = filePath
            });
        }
    }

    private void AnalyzeClass(ClassDeclarationSyntax classDecl, string filePath, AnalysisResult result)
    {
        var className = classDecl.Identifier.Text;
        var baseTypes = classDecl.BaseList?.Types.Select(t => t.ToString()) ?? Enumerable.Empty<string>();

        // Check for request handlers
        if (baseTypes.Any(t => t.Contains("IRequestHandler")))
        {
            result.HandlersFound++;

            // Check methods
            var methods = classDecl.Members.OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                if (method.Identifier.Text == "Handle")
                {
                    var returnType = method.ReturnType.ToString();

                    // Check if using Task instead of ValueTask
                    if (returnType.StartsWith("Task<") && !returnType.StartsWith("ValueTask<"))
                    {
                        result.Issues.Add(new MigrationIssue
                        {
                            Severity = IssueSeverity.Info,
                            Message = $"{className}.Handle will be converted to ValueTask for better performance",
                            Code = "TASK_TO_VALUETASK",
                            FilePath = filePath
                        });
                    }

                    // Check for CancellationToken
                    var hasToken = method.ParameterList.Parameters
                        .Any(p => p.Type?.ToString().Contains("CancellationToken") ?? false);

                    if (!hasToken)
                    {
                        result.Issues.Add(new MigrationIssue
                        {
                            Severity = IssueSeverity.Warning,
                            Message = $"{className}.Handle missing CancellationToken parameter",
                            Code = "MISSING_CANCELLATION_TOKEN",
                            FilePath = filePath
                        });
                    }
                }
            }
        }

        // Check for notification handlers
        if (baseTypes.Any(t => t.Contains("INotificationHandler")))
        {
            result.NotificationsFound++;
        }

        // Check for pipeline behaviors
        if (baseTypes.Any(t => t.Contains("IPipelineBehavior")))
        {
            result.PipelineBehaviorsFound++;
            result.HasCustomBehaviors = true;
        }

        // Check for custom IMediator
        if (baseTypes.Any(t => t == "IMediator"))
        {
            result.HasCustomMediator = true;
            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Warning,
                Message = $"Custom IMediator implementation found in {className} - may need manual updates",
                Code = "CUSTOM_MEDIATOR",
                FilePath = filePath
            });
        }
    }

    private void AnalyzeRecord(RecordDeclarationSyntax recordDecl, string filePath, AnalysisResult result)
    {
        var recordName = recordDecl.Identifier.Text;
        var baseTypes = recordDecl.BaseList?.Types.Select(t => t.ToString()) ?? Enumerable.Empty<string>();

        // Check for requests
        if (baseTypes.Any(t => t.Contains("IRequest")))
        {
            result.RequestsFound++;
            // Requests are compatible - no migration needed
        }

        // Check for notifications
        if (baseTypes.Any(t => t.Contains("INotification")))
        {
            result.NotificationsFound++;
            // Notifications are compatible - no migration needed
        }
    }

    /// <summary>
    /// Analyzes advanced MediatR patterns that may need special attention
    /// </summary>
    private void AnalyzeAdvancedPatterns(SyntaxNode root, string filePath, AnalysisResult result)
    {
        // 1. Detect streaming requests (IStreamRequest)
        DetectStreamingRequests(root, filePath, result);

        // 2. Detect custom pipeline behaviors with complex logic
        DetectCustomPipelineBehaviors(root, filePath, result);

        // 3. Detect service factory usage
        DetectServiceFactoryUsage(root, filePath, result);

        // 4. Detect generic constraints on handlers
        DetectGenericConstraints(root, filePath, result);

        // 5. Detect async enumerable patterns
        DetectAsyncEnumerablePatterns(root, filePath, result);

        // 6. Detect polymorphic request/response patterns
        DetectPolymorphicPatterns(root, filePath, result);

        // 7. Detect validation patterns (FluentValidation)
        DetectValidationPatterns(root, filePath, result);
    }

    private void DetectStreamingRequests(SyntaxNode root, string filePath, AnalysisResult result)
    {
        var streamRequests = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.BaseList?.Types.Any(t =>
                t.ToString().Contains("IStreamRequest")) ?? false)
            .ToList();

        foreach (var streamRequest in streamRequests)
        {
            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Warning,
                Message = $"Streaming request '{streamRequest.Identifier.Text}' detected - manual migration required",
                Code = "STREAM_REQUEST",
                FilePath = filePath,
                LineNumber = streamRequest.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }
    }

    private void DetectCustomPipelineBehaviors(SyntaxNode root, string filePath, AnalysisResult result)
    {
        var customBehaviors = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.BaseList?.Types.Any(t =>
                t.ToString().Contains("IPipelineBehavior")) ?? false)
            .ToList();

        foreach (var behavior in customBehaviors)
        {
            var behaviorName = behavior.Identifier.Text;

            // Check if it's a standard behavior or custom
            var isStandardBehavior = behaviorName.Contains("Logging") ||
                                   behaviorName.Contains("Validation") ||
                                   behaviorName.Contains("Transaction") ||
                                   behaviorName.Contains("Performance");

            if (!isStandardBehavior)
            {
                // Analyze complexity
                var methods = behavior.Members.OfType<MethodDeclarationSyntax>().ToList();
                var totalLines = methods.Sum(m => m.GetText().Lines.Count);

                if (totalLines > 50)
                {
                    result.Issues.Add(new MigrationIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Message = $"Complex custom pipeline behavior '{behaviorName}' ({totalLines} lines) - review migration carefully",
                        Code = "COMPLEX_BEHAVIOR",
                        FilePath = filePath,
                        LineNumber = behavior.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
                else
                {
                    result.Issues.Add(new MigrationIssue
                    {
                        Severity = IssueSeverity.Info,
                        Message = $"Custom pipeline behavior '{behaviorName}' will be migrated to Relay [Pipeline] attribute",
                        Code = "CUSTOM_BEHAVIOR",
                        FilePath = filePath,
                        LineNumber = behavior.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
        }
    }

    private void DetectServiceFactoryUsage(SyntaxNode root, string filePath, AnalysisResult result)
    {
        var serviceFactoryUsages = root.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.Text == "ServiceFactory")
            .ToList();

        if (serviceFactoryUsages.Any())
        {
            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Warning,
                Message = $"ServiceFactory usage detected ({serviceFactoryUsages.Count} occurrence(s)) - replace with IServiceProvider",
                Code = "SERVICE_FACTORY",
                FilePath = filePath,
                LineNumber = serviceFactoryUsages.First().GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }
    }

    private void DetectGenericConstraints(SyntaxNode root, string filePath, AnalysisResult result)
    {
        var constrainedClasses = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.TypeParameterList != null &&
                       c.ConstraintClauses.Any() &&
                       c.BaseList?.Types.Any(t => t.ToString().Contains("IRequestHandler")) == true)
            .ToList();

        foreach (var constrainedClass in constrainedClasses)
        {
            var constraints = string.Join(", ", constrainedClass.ConstraintClauses
                .SelectMany(cc => cc.Constraints.Select(c => c.ToString())));

            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Info,
                Message = $"Handler '{constrainedClass.Identifier.Text}' has generic constraints: {constraints} - verify after migration",
                Code = "GENERIC_CONSTRAINTS",
                FilePath = filePath,
                LineNumber = constrainedClass.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }
    }

    private void DetectAsyncEnumerablePatterns(SyntaxNode root, string filePath, AnalysisResult result)
    {
        var asyncEnumerableMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.ReturnType.ToString().Contains("IAsyncEnumerable"))
            .ToList();

        foreach (var method in asyncEnumerableMethods)
        {
            var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;

            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Info,
                Message = $"IAsyncEnumerable pattern in '{className}.{method.Identifier.Text}' - ensure compatibility with Relay",
                Code = "ASYNC_ENUMERABLE",
                FilePath = filePath,
                LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }
    }

    private void DetectPolymorphicPatterns(SyntaxNode root, string filePath, AnalysisResult result)
    {
        // Detect abstract or interface request types
        var abstractRequests = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => (c.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)) ||
                        c.Modifiers.Any(m => m.IsKind(SyntaxKind.InterfaceKeyword))) &&
                       c.BaseList?.Types.Any(t => t.ToString().Contains("IRequest")) == true)
            .ToList();

        foreach (var abstractRequest in abstractRequests)
        {
            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Warning,
                Message = $"Polymorphic request pattern '{abstractRequest.Identifier.Text}' - verify handler resolution in Relay",
                Code = "POLYMORPHIC_REQUEST",
                FilePath = filePath,
                LineNumber = abstractRequest.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }
    }

    private void DetectValidationPatterns(SyntaxNode root, string filePath, AnalysisResult result)
    {
        // Detect FluentValidation usage
        var validatorClasses = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.BaseList?.Types.Any(t =>
                t.ToString().Contains("AbstractValidator")) ?? false)
            .ToList();

        if (validatorClasses.Any())
        {
            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Info,
                Message = $"Found {validatorClasses.Count} FluentValidation validator(s) - compatible with Relay validation pipeline",
                Code = "FLUENT_VALIDATION",
                FilePath = filePath,
                LineNumber = validatorClasses.First().GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }

        // Detect validation pipeline behaviors
        var validationBehaviors = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.Identifier.Text.Contains("Validation") &&
                       c.BaseList?.Types.Any(t => t.ToString().Contains("IPipelineBehavior")) == true)
            .ToList();

        foreach (var validationBehavior in validationBehaviors)
        {
            result.Issues.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Info,
                Message = $"Validation pipeline behavior '{validationBehavior.Identifier.Text}' - can use Relay built-in validation",
                Code = "VALIDATION_BEHAVIOR",
                FilePath = filePath,
                LineNumber = validationBehavior.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }
    }
}
