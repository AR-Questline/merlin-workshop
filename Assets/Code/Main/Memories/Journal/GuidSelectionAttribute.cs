using System;
using System.Diagnostics;

namespace Awaken.TG.Main.Memories.Journal {
    [AttributeUsage(AttributeTargets.Field), Conditional("UNITY_EDITOR")]
    public class GuidSelectionAttribute : Attribute { }
}