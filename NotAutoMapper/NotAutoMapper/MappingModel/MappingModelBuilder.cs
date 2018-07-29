using Microsoft.CodeAnalysis;
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

            Func<MappingMemberInfo, (string name, ITypeSymbol type)> keySelector = m =>
            (
                name: (m.PropertyName ?? m.ConstructorArgumentName).ToUpperInvariant(),
                type: m.Type
            );

            var sourceLookup = sourceMembers.ToLookup(keySelector);
            var targetLookup = targetMembers.ToLookup(keySelector);

            var keys = sourceLookup.Concat(targetLookup).Select(x => x.Key).Distinct();

            var memberPairs = keys
                .Select(k => new MappingMemberPair
                (
                    source: sourceLookup[k].FirstOrDefault(),
                    target: targetLookup[k].FirstOrDefault(),
                    isImplemented: false
                ))
                .ToImmutableList();

            return new MappingTypeInfo
            (
                method: mapMethod,
                sourceType: sourceType,
                targetType: targetType,
                memberPairs: memberPairs
            );
        }

        public static IImmutableList<MappingMemberInfo> GetMemberInfos(ITypeSymbol type)
        {
            var members = type
                .GetMembers()
                .OfType<IMethodSymbol>()
                .ToArray();

            var getters = members
                .Where(m => m.MethodKind == MethodKind.PropertyGet)
                .Select(m =>
                (
                    name: m.AssociatedSymbol.Name,
                    type: m.ReturnType,
                    accessMode: AccessMode.Getter
                ));

            var setters = members
                .Where(m => m.MethodKind == MethodKind.PropertySet)
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
