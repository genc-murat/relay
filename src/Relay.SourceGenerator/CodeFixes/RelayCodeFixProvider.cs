using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.SourceGenerator.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RelayCodeFixProvider)), Shared]
    public class RelayCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return [DiagnosticDescriptors.HandlerMissingCancellationToken.Id]; }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var token = root.FindToken(diagnosticSpan.Start);
            var parent = token.Parent!;
            var declaration = parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
#pragma warning restore CS8602

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add CancellationToken parameter",
                    createChangedSolution: c => AddCancellationTokenAsync(context.Document, declaration, c),
                    equivalenceKey: "Add CancellationToken parameter"),
                diagnostic);
        }

        private async Task<Solution> AddCancellationTokenAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (oldRoot is not CompilationUnitSyntax compilationUnit)
            {
                // If not a compilation unit, just proceed with method change
                var paramList = methodDecl.ParameterList;
                var newParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                    .WithType(SyntaxFactory.ParseTypeName("System.Threading.CancellationToken"));

                var newParamList = paramList.AddParameters(newParam);
                var newMethod = methodDecl.WithParameterList(newParamList);

                var newRoot = oldRoot!.ReplaceNode(methodDecl, newMethod);
                return document.WithSyntaxRoot(newRoot).Project.Solution;
            }

            // Check if System.Threading is already imported
            var hasSystemThreading = compilationUnit.Usings.Any(u => u.Name?.ToString() == "System.Threading");

            var paramList2 = methodDecl.ParameterList;
            var newParam2 = SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(hasSystemThreading ? SyntaxFactory.ParseTypeName("CancellationToken") : SyntaxFactory.ParseTypeName("System.Threading.CancellationToken"));

            var newParamList2 = paramList2.AddParameters(newParam2);
            var newMethod2 = methodDecl.WithParameterList(newParamList2);

            var newRoot2 = oldRoot!.ReplaceNode(methodDecl, newMethod2);

            return document.WithSyntaxRoot(newRoot2).Project.Solution;
        }
    }
}
