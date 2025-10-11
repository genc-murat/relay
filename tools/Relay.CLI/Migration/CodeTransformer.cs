using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Relay.CLI.Migration;

/// <summary>
/// Transforms code files from MediatR to Relay patterns
/// </summary>
public class CodeTransformer
{
    public async Task<TransformationResult> TransformFileAsync(string filePath, string content, MigrationOptions options)
    {
        var result = new TransformationResult
        {
            FilePath = filePath,
            OriginalContent = content,
            NewContent = content
        };

        try
        {
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = await tree.GetRootAsync();

            // Transform using directives
            root = TransformUsingDirectives(root, result);

            // Transform handlers
            root = TransformHandlers(root, result, options);

            // Transform DI registrations
            root = TransformDIRegistrations(root, result);

            var newContent = root.ToFullString();

            result.NewContent = newContent;
            result.WasModified = newContent != content;

            if (result.WasModified)
            {
                var originalLines = content.Split('\n').Length;
                var newLines = newContent.Split('\n').Length;
                result.LinesChanged = Math.Abs(newLines - originalLines);
            }
        }
        catch (Exception ex)
        {
            result.Error = $"Transformation failed: {ex.Message}";
        }

        return result;
    }

    public async Task<TransformationResult> PreviewTransformAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        return await TransformFileAsync(filePath, content, new MigrationOptions());
    }

    private SyntaxNode TransformUsingDirectives(SyntaxNode root, TransformationResult result)
    {
        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
        
        foreach (var usingDir in usingDirectives)
        {
            var name = usingDir.Name?.ToString() ?? "";
            
            if (name == "MediatR" || name.StartsWith("MediatR."))
            {
                // Replace with Relay.Core
                var newUsing = SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName("Relay.Core")
                ).WithLeadingTrivia(usingDir.GetLeadingTrivia())
                 .WithTrailingTrivia(usingDir.GetTrailingTrivia());

                root = root.ReplaceNode(usingDir, newUsing);

                result.Changes.Add(new MigrationChange
                {
                    Category = "Using Directives",
                    Type = ChangeType.Modify,
                    Description = $"Changed 'using {name}' to 'using Relay.Core'",
                    FilePath = result.FilePath
                });
            }
        }

        return root;
    }

    private SyntaxNode TransformHandlers(SyntaxNode root, TransformationResult result, MigrationOptions options)
    {
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

        foreach (var classDecl in classes)
        {
            var baseTypes = classDecl.BaseList?.Types.Select(t => t.ToString()) ?? Enumerable.Empty<string>();

            if (!baseTypes.Any(t => t.Contains("IRequestHandler") || t.Contains("INotificationHandler")))
                continue;

            result.IsHandler = true;

            var methods = classDecl.Members.OfType<MethodDeclarationSyntax>().ToList();

            foreach (var method in methods)
            {
                if (method.Identifier.Text != "Handle")
                    continue;

                var newMethod = method;

                // 1. Rename Handle to HandleAsync
                if (!method.Identifier.Text.EndsWith("Async"))
                {
                    newMethod = newMethod.WithIdentifier(
                        SyntaxFactory.Identifier("HandleAsync")
                    );

                    result.Changes.Add(new MigrationChange
                    {
                        Category = "Method Signatures",
                        Type = ChangeType.Modify,
                        Description = $"Renamed Handle to HandleAsync in {classDecl.Identifier.Text}",
                        FilePath = result.FilePath
                    });
                }

                // 2. Convert Task to ValueTask
                var returnType = newMethod.ReturnType.ToString();
                if (returnType.StartsWith("Task<") && !returnType.StartsWith("ValueTask<"))
                {
                    var innerType = returnType.Substring(5, returnType.Length - 6); // Extract T from Task<T>
                    var newReturnType = SyntaxFactory.ParseTypeName($"ValueTask<{innerType}>");

                    newMethod = newMethod.WithReturnType(newReturnType);

                    result.Changes.Add(new MigrationChange
                    {
                        Category = "Return Types",
                        Type = ChangeType.Modify,
                        Description = $"Changed Task<{innerType}> to ValueTask<{innerType}> in {classDecl.Identifier.Text}",
                        FilePath = result.FilePath
                    });
                }

                // 3. Add [Handle] attribute if aggressive mode
                if (options.Aggressive && !method.AttributeLists.Any(al => al.ToString().Contains("Handle")))
                {
                    var handleAttribute = SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName("Handle"))
                        )
                    ).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

                    newMethod = newMethod.WithAttributeLists(
                        newMethod.AttributeLists.Add(handleAttribute)
                    );

                    result.Changes.Add(new MigrationChange
                    {
                        Category = "Attributes",
                        Type = ChangeType.Add,
                        Description = $"Added [Handle] attribute to {classDecl.Identifier.Text}.HandleAsync",
                        FilePath = result.FilePath
                    });
                }

                // Replace method if modified
                if (newMethod != method)
                {
                    root = root.ReplaceNode(method, newMethod);
                }
            }
        }

        return root;
    }

    private SyntaxNode TransformDIRegistrations(SyntaxNode root, TransformationResult result)
    {
        // Find AddMediatR calls and replace with AddRelay
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

        foreach (var invocation in invocations)
        {
            var expression = invocation.Expression.ToString();

            if (expression.Contains("AddMediatR"))
            {
                // Replace with AddRelay()
                var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    var newMemberAccess = memberAccess.WithName(
                        SyntaxFactory.IdentifierName("AddRelay")
                    );

                    // Remove arguments for simple case
                    var newInvocation = invocation
                        .WithExpression(newMemberAccess)
                        .WithArgumentList(SyntaxFactory.ArgumentList());

                    root = root.ReplaceNode(invocation, newInvocation);

                    result.Changes.Add(new MigrationChange
                    {
                        Category = "DI Registration",
                        Type = ChangeType.Modify,
                        Description = "Changed services.AddMediatR(...) to services.AddRelay()",
                        FilePath = result.FilePath
                    });
                }
            }
        }

        return root;
    }
}
