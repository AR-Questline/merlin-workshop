using System;
using System.Diagnostics;

namespace XNode.Attributes {
    [Conditional("UNITY_EDITOR")]
    public class OptionalPortAttribute : Attribute {
        public const string Infinite = "\u221E";
        public string DefaultValue { get; private set; }

        public OptionalPortAttribute(string defaultValue) {
            DefaultValue = defaultValue;
        }
    }
}