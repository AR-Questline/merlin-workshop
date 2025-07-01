using System;
using System.Diagnostics;

namespace Awaken.TG.Utility.Attributes.List {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true), Conditional("UNITY_EDITOR")]
    public class ListAttribute : Attribute {
        public ListEditOption listEditOption;

        public ListAttribute(ListEditOption listEditOption) {
            this.listEditOption = listEditOption;
        }
    }
}
