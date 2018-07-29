using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NotAutoMapper.MappingModel;

namespace NotAutoMapper
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NotAutoMapperAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NotAutoMapper";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var method = context.Symbol as IMethodSymbol;

            var mappingModel = MappingModelBuilder.GetTypeInfo(method);
            if (mappingModel == null)
                return;

            var startIndex = context.Symbol.Locations.First().SourceSpan.Start;
            var methodDeclaration = method.DeclaringSyntaxReferences.First();

            var semanticModel = context.Compilation.GetSemanticModel(methodDeclaration.SyntaxTree);

            string GetSymbolDisplayString(ISymbol symbol) => symbol.ToMinimalDisplayString(semanticModel, startIndex);

            if (mappingModel.MemberPairs.Any(member => !member.IsImplemented && member.Target != null && (member.Source != null || member.IsImplemented)))
            {
                context.ReportDiagnostic(Diagnostic.Create
                (
                    Rule,
                    method.Locations[0],
                    GetSymbolDisplayString(mappingModel.SourceType),
                    GetSymbolDisplayString(mappingModel.TargetType)
                ));
            }
        }
    }
}
