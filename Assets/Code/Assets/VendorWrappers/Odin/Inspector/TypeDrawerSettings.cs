using System;

namespace Sirenix.OdinInspector
{
    public class TypeDrawerSettingsAttribute : Attribute
    {
        public Type BaseType;
        public TypeInclusionFilter Filter = TypeInclusionFilter.IncludeAll;
    }
    
    [Flags]
    public enum TypeInclusionFilter
    {
        None = 0,
        IncludeConcreteTypes = 1,
        IncludeGenerics = 2,
        IncludeAbstracts = 4,
        IncludeInterfaces = 8,
        IncludeAll = IncludeInterfaces | IncludeAbstracts | IncludeGenerics | IncludeConcreteTypes, // 0x0000000F
    }
}