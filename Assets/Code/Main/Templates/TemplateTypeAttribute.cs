using System;
using System.Diagnostics;

namespace Awaken.TG.Main.Templates {
    [Conditional("UNITY_EDITOR")]
    public class TemplateTypeAttribute : Attribute {
        public Type Type { get; private set; }

        public TemplateTypeAttribute(Type type) {
            Type = type;
        }
    }
}