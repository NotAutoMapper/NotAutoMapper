using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace NotAutoMapper.MappingModel
{
    public class MappingTypeInfo
    {
        public MappingTypeInfo(TypeInfo sourceType, TypeInfo targetType, IImmutableList<MappingMemberPair> memberPairs)
        {
            SourceType = sourceType;
            TargetType = targetType;
            MemberPairs = memberPairs ?? throw new ArgumentNullException(nameof(memberPairs));
        }

        public TypeInfo SourceType { get; }
        public TypeInfo TargetType { get; }
        public IImmutableList<MappingMemberPair> MemberPairs { get; }
    }
}
