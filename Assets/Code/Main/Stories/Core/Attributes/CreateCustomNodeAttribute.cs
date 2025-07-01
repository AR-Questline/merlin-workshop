using System;
using System.Diagnostics;

namespace Awaken.TG.Main.Stories.Core.Attributes {
    [Conditional("UNITY_EDITOR")]
    [UnityEngine.Scripting.Preserve]
    public class CreateCustomNodeAttribute : Attribute {
        [UnityEngine.Scripting.Preserve] public string title;
        [UnityEngine.Scripting.Preserve] public Type type;

        public CreateCustomNodeAttribute(string title, Type type) {
            this.title = title;
            this.type = type;
        }
    }
}