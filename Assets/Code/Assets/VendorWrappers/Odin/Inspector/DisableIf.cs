using System;

namespace Sirenix.OdinInspector
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class DisableIfAttribute : Attribute
    {
        public DisableIfAttribute(string condition, bool animate = true) { }
        public DisableIfAttribute(string condition, object optionalValue, bool animate = true) { }
    }
}