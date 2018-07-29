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

            var parameterType = semanticModel.GetTypeInfo(parameter.Type);
            var returnType = semanticModel.GetTypeInfo(oldMethod.ReturnType);

            var mappingModel = MappingModelBuilder.GetTypeInfo
            (
                sourceType: parameterType.ConvertedType,
                targetType: returnType.ConvertedType
            );

            var argumentList = GetArgumentList
            (
                sourceName: parameter.Identifier.Text,
                typeInfo: mappingModel
            );

            var newMethod = oldMethod.WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression
            (
                type: oldMethod.ReturnType,
                argumentList: argumentList,
                initializer: null
            )))).WithAdditionalAnnotations(Formatter.Annotation);

            return newMethod;
        }

        private ArgumentListSyntax GetArgumentList(string sourceName, MappingTypeInfo typeInfo)
        {
            var arguments = typeInfo.MemberPairs.Select(m => GetArgument(sourceName, m));

            var sb = new StringBuilder();
            sb.Append("\r\n (\r\n");

            foreach (var arg in arguments)
                sb.Append("     " + arg.ToString() + "\r\n");

            sb.Append(" )");
            var tet = sb.ToString();

            return SyntaxFactory.ParseArgumentList(sb.ToString());
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
