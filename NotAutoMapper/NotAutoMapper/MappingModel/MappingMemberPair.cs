namespace NotAutoMapper.MappingModel
{
    public class MappingMemberPair
    {
        public MappingMemberPair(MappingMemberInfo source, MappingMemberInfo target, bool isImplemented)
        {
            Source = source;
            Target = target;
            IsImplemented = isImplemented;
        }

        public MappingMemberInfo Source { get; }
        public MappingMemberInfo Target { get; }
        public bool IsImplemented { get; }
    }
}
