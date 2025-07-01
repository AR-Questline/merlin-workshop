using System;

namespace Awaken.TG.Editor.Main.Templates.Presets {
    [AttributeUsage(AttributeTargets.Method)]
    public class PresetAttribute : Attribute {
        public Type ObjectType { get; }

        public PresetAttribute(Type objectType) {
            ObjectType = objectType;
        }
    }
}