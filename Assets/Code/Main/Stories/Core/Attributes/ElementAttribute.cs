using System;
using JetBrains.Annotations;
using System.Diagnostics;

namespace Awaken.TG.Main.Stories.Core.Attributes {
    [AttributeUsage(validOn: AttributeTargets.Class, Inherited = false), Conditional("UNITY_EDITOR"), MeansImplicitUse]
    public class ElementAttribute : Attribute {
        public string name;

        public ElementAttribute(string name) {
            this.name = name;
        }
    }
}