using System;
using Microsoft.CodeAnalysis;

namespace NotAutoMapper.MappingModel
{
    [Flags]
    public enum AccessMode
    {
        None = 0,
        Getter = 1,
        Setter = 2,
        ConstructorArgument = 4
    }

    public class MappingMemberInfo
    {
        public MappingMemberInfo(string propertyName, string constructorArgumentName, ITypeSymbol type, AccessMode accessMode)
        {
            PropertyName = propertyName;
            ConstructorArgumentName = constructorArgumentName;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            AccessMode = accessMode;
        }

        public string PropertyName { get; }
        public string ConstructorArgumentName { get; }
        public ITypeSymbol Type { get; }
        public AccessMode AccessMode { get; }
        public bool HasConstructorArgument { get { return AccessMode.HasFlag(AccessMode.ConstructorArgument); } }
        public bool HasGetter { get { return AccessMode.HasFlag(AccessMode.Getter); } }
        public bool HasSetter { get { return AccessMode.HasFlag(AccessMode.Setter); } }
    }
}
