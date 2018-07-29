using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace NotAutoMapper.MappingModel
{
    public class MappingTypeInfo
    {
        public MappingTypeInfo(IMethodSymbol method, ITypeSymbol sourceType, ITypeSymbol targetType, IImmutableList<MappingMemberPair> memberPairs)
        {
            Method = method;
            SourceType = sourceType;
            TargetType = targetType;
            MemberPairs = memberPairs ?? throw new ArgumentNullException(nameof(memberPairs));
        }

        public IMethodSymbol Method { get; }
        public ITypeSymbol SourceType { get; }
        public ITypeSymbol TargetType { get; }
        public IImmutableList<MappingMemberPair> MemberPairs { get; }
    }
}
