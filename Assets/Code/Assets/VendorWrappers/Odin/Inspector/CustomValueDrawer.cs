using System;

namespace Sirenix.OdinInspector
{
    public class CustomValueDrawerAttribute : Attribute
    {
        public string Action;
        public string MethodName
        {
            get => this.Action;
            set => this.Action = value;
        }
        
        public CustomValueDrawerAttribute(string action) => this.Action = action;
    }
}