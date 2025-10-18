using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.SourceGenerator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RelayCodeFixProvider)), Shared]
    public class RelayCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.HandlerMissingCancellationToken.Id); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add CancellationToken parameter",
                    createChangedSolution: c => AddCancellationTokenAsync(context.Document, declaration, c),
                    equivalenceKey: "Add CancellationToken parameter"),
                diagnostic);
        }

        private async Task<Solution> AddCancellationTokenAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var parameterList = methodDecl.ParameterList;
            var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.ParseTypeName("CancellationToken"));

            var newParameterList = parameterList.AddParameters(newParameter);
            var newMethodDecl = methodDecl.WithParameterList(newParameterList);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(methodDecl, newMethodDecl);

            return document.WithSyntaxRoot(newRoot).Project.Solution;
        }
    }
}
