using System;
using JetBrains.Annotations;

namespace Awaken.TG.Editor.Main.Stories.Drawers {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false), MeansImplicitUse]
    public class CustomElementEditorAttribute : Attribute {
        public Type type;

        public CustomElementEditorAttribute(Type type) {
            this.type = type;
        }
    }
}