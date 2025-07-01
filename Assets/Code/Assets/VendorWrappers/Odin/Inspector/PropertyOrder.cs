using System;

namespace Sirenix.OdinInspector
{
    public class PropertyOrderAttribute : Attribute
    {
        public PropertyOrderAttribute() { }
        public PropertyOrderAttribute(float order) { }
    }
}