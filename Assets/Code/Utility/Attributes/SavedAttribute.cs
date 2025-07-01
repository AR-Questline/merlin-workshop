using System;

namespace Awaken.TG.Utility.Attributes {
    /// <summary>
    /// Used to mark fields and properties that need to be serialized in model (everything else is ignored on serialization)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class SavedAttribute : Attribute {
        public SavedAttribute() { }
        public SavedAttribute(bool @default) { }
        public SavedAttribute(int @default) { }
        public SavedAttribute(long @default) { }
        public SavedAttribute(float @default) { }
        public SavedAttribute(string @default) { }
    }
}