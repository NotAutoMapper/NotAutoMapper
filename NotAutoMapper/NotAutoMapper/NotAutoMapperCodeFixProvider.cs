using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using NotAutoMapper.MappingModel;

namespace NotAutoMapper
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NotAutoMapperCodeFixProvider)), Shared]
    public class NotAutoMapperCodeFixProvider : CodeFixProvider
    {
        private const string title = "Complete Map method";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(NotAutoMapperAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var methodDeclaration = root
                .FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: token => CreateMapBody(context, methodDeclaration, token),
                    equivalenceKey: title
                    ),
                diagnostic);
        }

        private SyntaxTrivia GetLineBreakTrivia(SyntaxNode root)
        {
            var trivia = root
                .DescendantTokens()
                .SelectMany(token => token.TrailingTrivia)
                .FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

            if (trivia == default(SyntaxTrivia))
                trivia = SyntaxFactory.CarriageReturnLineFeed;

            return trivia;
        }


        private async Task<Document> CreateMapBody(CodeFixContext context, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var linebreak = GetLineBreakTrivia(oldRoot);
            var linebreakSpace = new[] { linebreak, SyntaxFactory.Whitespace(" ") };

            var model = await context.Document.GetSemanticModelAsync().ConfigureAwait(false);

            var parameter = methodDeclaration.ParameterList.Parameters[0] as ParameterSyntax;
            var parameterType = model.GetTypeInfo(parameter.Type);
            var returnType = model.GetTypeInfo(methodDeclaration.ReturnType);

            var sourceParameterName = parameter.Identifier.Text;

            var mappingModel = MappingModelBuilder.GetTypeInfo(parameterType.ConvertedType, returnType.ConvertedType);

            var argumentList = GetArgumentList(sourceParameterName, mappingModel);

            var newMethod = methodDeclaration.WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression
            (
                type: methodDeclaration.ReturnType,
                argumentList: argumentList,
                initializer: null
            )))).WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = oldRoot.ReplaceNode(methodDeclaration, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }

        private ArgumentListSyntax GetArgumentList(string sourceName, MappingTypeInfo typeInfo)
        {
            var arguments = typeInfo.MemberPairs.Select(m => GetArgument(sourceName, m));

            return SyntaxFactory.ParseArgumentList("(" + string.Join(",\n", arguments) + ")");
        }

        private ArgumentSyntax GetArgument(string sourceName, MappingMemberPair member)
        {
            var expression = SyntaxFactory.ParseExpression($"{sourceName}.{member.Source.PropertyName}");

            return SyntaxFactory.Argument
            (
                nameColon: SyntaxFactory.NameColon(member.Target.ConstructorArgumentName),
                refOrOutKeyword: SyntaxFactory.Token(SyntaxKind.None),
                expression: expression
            );
        }
    }
}
