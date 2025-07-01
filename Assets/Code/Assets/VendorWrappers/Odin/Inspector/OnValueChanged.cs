using System;

namespace Sirenix.OdinInspector
{
    public class OnValueChangedAttribute : Attribute
    {
        public string Action;
        public bool IncludeChildren;
        public bool InvokeOnUndoRedo = true;
        public bool InvokeOnInitialize;
        
        public OnValueChangedAttribute(string action, bool includeChildren = false) { }
    }
}