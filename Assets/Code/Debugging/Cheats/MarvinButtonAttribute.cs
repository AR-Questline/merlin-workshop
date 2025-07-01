using System;
using JetBrains.Annotations;

namespace Awaken.TG.Debugging.Cheats {
    [AttributeUsage(AttributeTargets.Method), MeansImplicitUse]
    public class MarvinButtonAttribute : Attribute {
        public string Visible { get; }
        public string State { get; }

        public MarvinButtonAttribute(string visible = null, string state = null) {
            Visible = visible;
            State = state;
        }
    }
}