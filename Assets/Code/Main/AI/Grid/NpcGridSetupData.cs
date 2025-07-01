using System;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.AI.Grid {
    [Serializable]
    public struct NpcGridSetupData {
        public int gridHalfSize;
        public float chunkSize;
        public float hysteresis;
        
        [ShowInInspector] float GridExtents => gridHalfSize * chunkSize;
    }
}