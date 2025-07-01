using System;

namespace Sirenix.OdinInspector
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class ShowIfAttribute : Attribute
    {
        public string Condition;
        
        public ShowIfAttribute(string condition, bool animate = true) { }
        public ShowIfAttribute(string condition, object optionalValue, bool animate = true) { }
    }
}