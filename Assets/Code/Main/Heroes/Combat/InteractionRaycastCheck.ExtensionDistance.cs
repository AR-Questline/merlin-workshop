using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Combat {
    public class ExtensionDistance : RichEnum {
        public readonly float distance;

        protected ExtensionDistance(string enumName, float distance, string inspectorCategory = "") : base(enumName, inspectorCategory) {
            this.distance = distance;
        }

        [UnityEngine.Scripting.Preserve] public static readonly ExtensionDistance Short = new(nameof(Short), 1F);
        [UnityEngine.Scripting.Preserve] public static readonly ExtensionDistance Mid = new(nameof(Mid), 3F);
        [UnityEngine.Scripting.Preserve] public static readonly ExtensionDistance Long = new(nameof(Long), 6F);
        [UnityEngine.Scripting.Preserve] public static readonly ExtensionDistance VeryLong = new(nameof(VeryLong), 12F);
    }
}