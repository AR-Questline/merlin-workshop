using System;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    [Serializable]
    public struct MaterialMeshVisibilityStats {
        public uint anyInstanceVisibleFramesCount;

        public MaterialMeshVisibilityStats(uint anyInstanceVisibleFramesCount) {
            this.anyInstanceVisibleFramesCount = anyInstanceVisibleFramesCount;
        }
    }
}