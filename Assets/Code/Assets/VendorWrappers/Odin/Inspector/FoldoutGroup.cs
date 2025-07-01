using System;

namespace Sirenix.OdinInspector
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class FoldoutGroupAttribute : GroupAttribute
    {
        public bool Expanded { get => default; set { } }
        public bool HasDefinedExpanded { get => default; set { } }
        
        public FoldoutGroupAttribute(string groupName, float order = 0.0f) { }
        public FoldoutGroupAttribute(string groupName, bool expanded, float order = 0.0f) { }
    }
}