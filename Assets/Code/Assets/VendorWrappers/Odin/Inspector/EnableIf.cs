using System;

namespace Sirenix.OdinInspector
{
    public class EnableIfAttribute : Attribute
    {
        public EnableIfAttribute(string condition, bool animate = true) { }
        public EnableIfAttribute(string condition, object optionalValue, bool animate = true) { }
    }
}