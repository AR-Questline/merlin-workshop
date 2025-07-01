using System;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Character.Features.Config {
    [Serializable]
    public struct BlendShapeGroup {
#if UNITY_EDITOR
        public string groupCategory;
#endif
        public string[] blendShapes;
        public BlendshapesRandomizerMode mode;
        [ShowIf(nameof(MultipleToSelect))] public int quantityToRandomize;

        bool MultipleToSelect => mode.HasFlagFast(BlendshapesRandomizerMode.Multiple);

        [Flags]
        public enum BlendshapesRandomizerMode {
            [UnityEngine.Scripting.Preserve] None = 0,
            One = 1 << 0,
            Multiple = 1 << 1,
            ContinuousValue = 1 << 2,
        }
    }
}
