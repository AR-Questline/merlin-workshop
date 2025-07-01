using System;

namespace Sirenix.OdinInspector
{
    public class PropertySpaceAttribute : Attribute
    {
        public float SpaceAfter;
        public float SpaceBefore;
        
        public PropertySpaceAttribute(float spaceBefore = 5, float spaceAfter = 0) { }
    }
}