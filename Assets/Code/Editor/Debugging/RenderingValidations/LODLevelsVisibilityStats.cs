using Awaken.Utility.Debugging;
using Unity.Mathematics;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    [System.Serializable]
    public unsafe struct LODLevelsVisibilityStats {
        public const int LODLevelsCount = 8;
        
        public fixed uint lodAnyInstanceVisibleFramesCount[LODLevelsCount];
        public MaterialMeshVisibilityStats GetMaxVisibilityStats() {
            int maxStatsIndex = 0;
            uint maxStats = lodAnyInstanceVisibleFramesCount[0];
            for (int i = 1; i < 8; i++) {
                if (lodAnyInstanceVisibleFramesCount[i] > maxStats) {
                    maxStats = lodAnyInstanceVisibleFramesCount[i];
                    maxStatsIndex = i;
                }
            }
            return new MaterialMeshVisibilityStats(lodAnyInstanceVisibleFramesCount[maxStatsIndex]);
        }

        public void SetStatsForLOD(int lodLevel, MaterialMeshVisibilityStats visibilityStats) {
            if (lodLevel < 0 || lodLevel >= LODLevelsCount) {
                Log.Minor?.Error($"Lod level {lodLevel} not valid");
                return;
            }
            lodAnyInstanceVisibleFramesCount[lodLevel] = visibilityStats.anyInstanceVisibleFramesCount;
        }
    }
}