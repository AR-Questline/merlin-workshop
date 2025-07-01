using System;
using NUnit.Framework.Constraints;

namespace Sirenix.OdinInspector
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class HideIfAttribute : Attribute
    {
        public string Condition;
        
        public HideIfAttribute(string condition, bool animate = true) { }
        public HideIfAttribute(string condition, object optionalValue, bool animate = true) { }
    }
}