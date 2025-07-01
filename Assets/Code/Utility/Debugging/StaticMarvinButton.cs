using System;
using JetBrains.Annotations;

namespace Awaken.Utility.Debugging {
    /// <summary>
    /// Marked method must be in NonPublic static class and the method must be static.
    /// <remarks>
    /// <see cref="Visible"/> and <see cref="State"/> must be method, cannot be property.
    /// </remarks>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method), MeansImplicitUse]
    public class StaticMarvinButtonAttribute : Attribute {
        public string Visible { get; }
        public string State { get; }

        public StaticMarvinButtonAttribute(string visible = null, string state = null) {
            Visible = visible;
            State = state;
        }
    }
}
