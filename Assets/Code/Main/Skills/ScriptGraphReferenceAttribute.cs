using System;

namespace Awaken.TG.Main.Skills {
    /// <summary>
    /// Attribute used on a field that should be a ScriptGraphAsset but due to VS heavy deserialization we don't want to use direct references to it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ScriptGraphReferenceAttribute : Attribute {
    }
}