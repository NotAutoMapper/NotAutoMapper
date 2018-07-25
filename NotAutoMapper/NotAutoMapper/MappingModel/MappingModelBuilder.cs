using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NotAutoMapper.MappingModel
{
    public static class MappingModelBuilder
    {
        public static MappingTypeInfo GetTypeInfo(TypeInfo sourceType, TypeInfo targetType)
        {
            var sourceMembers = GetMemberInfos(sourceType);
            var targetMembers = GetMemberInfos(targetType);

            Func<MappingMemberInfo, (string name, ITypeSymbol type)> keySelector = m =>
                (name: (m.PropertyName ?? m.ConstructorArgumentName).ToUpperInvariant(), type: m.Type);

            var sourceLookup = sourceMembers.ToLookup(m => keySelector(m));
            var targetLookup = targetMembers.ToLookup(m => keySelector(m));

            var keys = sourceLookup.Select(x => x.Key).Concat(targetLookup.Select(x => x.Key)).Distinct();

            var memberPairs = keys
                .Select(k => new MappingMemberPair(sourceLookup[k].FirstOrDefault(), targetLookup[k].FirstOrDefault(), false))
                .ToImmutableList();

            return new MappingTypeInfo
            (
                sourceType: sourceType,
                targetType: targetType,
                memberPairs: memberPairs
            );
        }

        public static IImmutableList<MappingMemberInfo> GetMemberInfos(TypeInfo type)
        {
            var members = type.ConvertedType
                .GetMembers()
                .OfType<IMethodSymbol>()
                .ToArray();

            var getters = members
                .Where(m => m.MethodKind == MethodKind.PropertyGet)
                .Select(m => (
                    name: m.AssociatedSymbol.Name,
                    type: m.ReturnType,
                    accessMode: AccessMode.Getter
                ));

            var setters = members
                .Where(m => m.MethodKind == MethodKind.PropertySet)
                .Select(m => (
                    name: m.AssociatedSymbol.Name,
                    type: m.Parameters[0].Type,
                    accessMode: AccessMode.Setter
                ));

            var constructorArguments = members
                .SingleOrDefault(m => m.MethodKind == MethodKind.Constructor && m.Parameters.Length > 0)
                ?.Parameters.Select(m => (
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
