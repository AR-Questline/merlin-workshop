using System;

namespace Sirenix.OdinInspector
{
    public class OnInspectorInitAttribute : Attribute
    {
        public OnInspectorInitAttribute(string method) { }
    }
}