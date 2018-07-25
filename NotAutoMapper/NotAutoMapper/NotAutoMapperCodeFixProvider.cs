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

        private SyntaxTrivia LineBreakTrivia
        {
            get
            {
                switch (FormattingOptions.NewLine.DefaultValue)
                {
                    case "\r": return SyntaxFactory.CarriageReturn;
                    case "\r\n": return SyntaxFactory.CarriageReturnLineFeed;
                    case "\n": return SyntaxFactory.LineFeed;

                    default:
                        throw new InvalidOperationException($"Unknown newline option: {FormattingOptions.NewLine.DefaultValue}.");
                }
            }
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

            var editor = await Microsoft.CodeAnalysis.Editing.DocumentEditor.CreateAsync(document);

            var model = await context.Document.GetSemanticModelAsync().ConfigureAwait(false);

            var parameter = methodDeclaration.ParameterList.Parameters[0] as ParameterSyntax;
            var parameterType = model.GetTypeInfo(parameter.Type);
            var returnType = model.GetTypeInfo(methodDeclaration.ReturnType);

            var parameterProperties = parameterType.ConvertedType.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();
            var returnParameters = returnType.ConvertedType.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor).Parameters;

            var sourceParameterName = parameter.Identifier.Text;

            var mappingModel = MappingModelBuilder.GetTypeInfo(parameterType, returnType);
            var mapping = GetMap(parameterType, returnType).ToList();

            var arguments = mapping.Select(x => GetArgument(x.Paramter, x.Property, sourceParameterName));

            var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments));
            argumentList = argumentList.WithLeadingTrivia(linebreakSpace).WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithLeadingTrivia(linebreakSpace));
            var newMethod = methodDeclaration.WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression
            (
                type: methodDeclaration.ReturnType,
                argumentList: argumentList,
                initializer: null
            )))).WithTrailingTrivia(linebreak).WithAdditionalAnnotations(Formatter.Annotation);

            editor.ReplaceNode(methodDeclaration, newMethod);

            return editor.GetChangedDocument();
        }

        private IEnumerable<(IPropertySymbol Property, IParameterSymbol Paramter)> GetMap(TypeInfo fromType, TypeInfo toType)
        {
            var constructor = toType.ConvertedType
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Constructor && m.Parameters.Length > 0)
                .SingleOrDefault();

            if (constructor == null)
                yield break;

            var properties = fromType.ConvertedType.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();
            foreach (var parameter in constructor.Parameters)
            {
                var property = properties.FirstOrDefault(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));

                if (parameter.Type == property.Type)
                    yield return (property, parameter);
            }
        }

        private ArgumentSyntax GetArgument(IParameterSymbol parameter, IPropertySymbol property, string sourceName)
        {
            var newline = new[] { LineBreakTrivia, SyntaxFactory.Whitespace(new string(' ', FormattingOptions.TabSize.DefaultValue + 1)) };
            var namecolon = SyntaxFactory.NameColon(parameter.Name);

            var memberAccess = SyntaxFactory.MemberAccessExpression
            (
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: SyntaxFactory.IdentifierName(sourceName),
                operatorToken: SyntaxFactory.Token(SyntaxKind.DotToken),
                name: SyntaxFactory.IdentifierName(property.Name)
            );

            return SyntaxFactory.Argument
            (
                nameColon: namecolon,
                refOrOutKeyword: SyntaxFactory.Token(SyntaxKind.None),
                expression: memberAccess
            ).WithLeadingTrivia(newline);
        }
    }
}
