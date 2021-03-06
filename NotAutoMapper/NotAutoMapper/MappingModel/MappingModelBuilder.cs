﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace NotAutoMapper.MappingModel
{
    public static class MappingModelBuilder
    {
        public static MappingTypeInfo GetTypeInfo(IMethodSymbol mapMethod)
        {
            if (mapMethod == null)
                return null;

            if (!mapMethod.Name.Equals("Map", StringComparison.OrdinalIgnoreCase))
                return null;

            if (mapMethod.ReturnsVoid || mapMethod.IsAsync || mapMethod.IsGenericMethod)
                return null;

            if (mapMethod.Parameters.IsEmpty)
                return null;

            var parameterType = mapMethod.Parameters.First().Type;
            var returnType = mapMethod.ReturnType;

            return GetTypeInfo(mapMethod, parameterType, returnType);
        }
        private static MappingTypeInfo GetTypeInfo(IMethodSymbol mapMethod, ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            var sourceMembers = GetMemberInfos(sourceType);
            var targetMembers = GetMemberInfos(targetType);

            var memberPairs = GetMappingPairs
            (
                sourceMembers: sourceMembers,
                targetMembers: targetMembers
            );

            var mappedParameters = GetMappedParameters(mapMethod);
            memberPairs = memberPairs.Select(mp => new MappingMemberPair
            (
                source: mp.Source,
                target: mp.Target,
                isImplemented: mappedParameters.Contains(mp.Target?.ConstructorArgumentName)
            )).ToImmutableList();

            return new MappingTypeInfo
            (
                method: mapMethod,
                sourceType: sourceType,
                targetType: targetType,
                memberPairs: memberPairs
            );
        }

        private static IImmutableList<string> GetMappedParameters(IMethodSymbol mapMethod)
        {
            var methodSyntax = mapMethod.DeclaringSyntaxReferences.First().GetSyntax() as MethodDeclarationSyntax;
            var lastStatement = methodSyntax.Body?.Statements.LastOrDefault();

            if (lastStatement is ReturnStatementSyntax ret && ret.Expression is ObjectCreationExpressionSyntax cre)
            {
                return cre
                    .ArgumentList
                    .Arguments
                    .Select(arg => arg.NameColon)
                    .Where(n => n != null)
                    .Select(n => n.Name.Identifier.Text)
                    .ToImmutableList();
            }

            return ImmutableList<string>.Empty;
        }

        private static IImmutableList<MappingMemberPair> GetMappingPairs(IImmutableList<MappingMemberInfo> sourceMembers, IImmutableList<MappingMemberInfo> targetMembers)
        {
            (string name, ITypeSymbol type) keySelector(MappingMemberInfo m) =>
            (
                name: (m.PropertyName ?? m.ConstructorArgumentName).ToUpperInvariant(),
                type: m.Type
            );

            var sourceLookup = sourceMembers.ToLookup(keySelector);
            var targetLookup = targetMembers.ToLookup(keySelector);

            var keys = sourceLookup.Concat(targetLookup).Select(x => x.Key).Distinct();

            return keys
                .Select(k => new MappingMemberPair
                (
                    source: sourceLookup[k].FirstOrDefault(),
                    target: targetLookup[k].FirstOrDefault(),
                    isImplemented: false
                ))
                .ToImmutableList();
        }

        public static IImmutableList<MappingMemberInfo> GetMemberInfos(ITypeSymbol type)
        {
            var members = type
                .GetMembers()
                .OfType<IMethodSymbol>()
                .ToArray();

            var getters = members
                .Where(m => m.MethodKind == MethodKind.PropertyGet && !m.IsStatic)
                .Select(m =>
                (
                    name: m.AssociatedSymbol.Name,
                    type: m.ReturnType,
                    accessMode: AccessMode.Getter
                ));

            var setters = members
                .Where(m => m.MethodKind == MethodKind.PropertySet && !m.IsStatic)
                .Select(m =>
                (
                    name: m.AssociatedSymbol.Name,
                    type: m.Parameters[0].Type,
                    accessMode: AccessMode.Setter
                ));

            var constructorArguments = members
                .SingleOrDefault(m => m.MethodKind == MethodKind.Constructor && m.Parameters.Length > 0)
                ?.Parameters.Select(m =>
                (
                    name: m.Name,
                    type: m.Type,
                    accessMode: AccessMode.ConstructorArgument
                ))
                ?? Enumerable.Empty<(string name, ITypeSymbol type, AccessMode accessMode)>();

            return getters.Concat(setters).Concat(constructorArguments)
                .GroupBy(m => (name: m.name.ToUpperInvariant(), type: m.type))
                .Select(g => new MappingMemberInfo
                (
                    propertyName: g.FirstOrDefault(m => (AccessMode.Getter | AccessMode.Setter).HasFlag(m.accessMode)).name,
                    constructorArgumentName: g.FirstOrDefault(m => m.accessMode == AccessMode.ConstructorArgument).name,
                    type: g.Key.type,
                    accessMode: g.Aggregate(AccessMode.None, (r, am) => r | am.accessMode)
                ))
                .ToImmutableList();
        }
    }
}
