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
using System.Text;

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
                    createChangedDocument: token => ReplaceMapMethod(context, methodDeclaration, token),
                    equivalenceKey: title
                    ),
                diagnostic);
        }

        private async Task<Document> ReplaceMapMethod(CodeFixContext context, MethodDeclarationSyntax oldMethod, CancellationToken cancellationToken)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken);
            var newMethod = CreateMapMethod(oldMethod, semanticModel);

            var root = await context.Document.GetSyntaxRootAsync(cancellationToken);

            return context.Document.WithSyntaxRoot(root.ReplaceNode(oldMethod, newMethod));
        }

        private MethodDeclarationSyntax CreateMapMethod(MethodDeclarationSyntax oldMethod, SemanticModel semanticModel)
        {
            var parameter = oldMethod.ParameterList.Parameters[0] as ParameterSyntax;

            var mappingModel = MappingModelBuilder.GetTypeInfo(semanticModel.GetDeclaredSymbol(oldMethod));

            var preMapped = GetExistingArguments(oldMethod);
            var argumentList = GetArgumentList
            (
                sourceName: parameter.Identifier.Text,
                typeInfo: mappingModel,
                existingArguments: preMapped
            );

            var newMethod = oldMethod.WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression
            (
                type: oldMethod.ReturnType,
                argumentList: argumentList,
                initializer: null
            )))).WithAdditionalAnnotations(Formatter.Annotation);

            return newMethod;
        }

        private ImmutableDictionary<string, ExpressionSyntax> GetExistingArguments(MethodDeclarationSyntax methodSyntax)
        {
            var lastStatement = methodSyntax.Body?.Statements.LastOrDefault();

            if (lastStatement is ReturnStatementSyntax ret && ret.Expression is ObjectCreationExpressionSyntax cre)
            {
                return cre
                    .ArgumentList
                    .Arguments
                    .Where(n => n.NameColon != null)
                    .ToImmutableDictionary(x => x.NameColon.Name.Identifier.Text, x => x.Expression);
            }

            return ImmutableDictionary<string, ExpressionSyntax>.Empty;
        }

        private ArgumentListSyntax GetArgumentList(string sourceName, MappingTypeInfo typeInfo, ImmutableDictionary<string, ExpressionSyntax> existingArguments)
        {
            var arguments = typeInfo
                .MemberPairs
                .Where(arg => arg.Target != null)
                .Select(m => GetArgument(sourceName, m, existingArguments))
                .Where(x => x != null)
                .ToImmutableList();

            var sb = new StringBuilder();
            sb.Append("\r\n (\r\n");

            for (int i = 0; i < arguments.Count; i++)
            {
                sb.Append("     " + arguments[i].ToString());
                if (i < arguments.Count - 1)
                    sb.Append(",");
                sb.Append("\r\n");
            }

            sb.Append(" )");
            var tet = sb.ToString();

            return SyntaxFactory.ParseArgumentList(sb.ToString());
        }

        private ArgumentSyntax GetArgument(string sourceName, MappingMemberPair member, ImmutableDictionary<string, ExpressionSyntax> existingArguments)
        {
            if (!existingArguments.TryGetValue(member.Target.ConstructorArgumentName, out ExpressionSyntax expression))
            {
                if (member.Source != null)
                    expression = SyntaxFactory.ParseExpression($"{sourceName}.{member.Source.PropertyName}");
            }

            if (expression == null)
                return null;

            return SyntaxFactory.Argument
            (
                nameColon: SyntaxFactory.NameColon(member.Target.ConstructorArgumentName),
                refOrOutKeyword: SyntaxFactory.Token(SyntaxKind.None),
                expression: expression
            );
        }
    }
}
