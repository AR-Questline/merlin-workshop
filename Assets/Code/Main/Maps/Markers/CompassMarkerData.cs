using System;

namespace Awaken.TG.Main.Maps.Markers {
    [Serializable]
    public struct CompassMarkerData {
        public CompassMarkerType compassMarkerType;
        public float nearDistance;
        public float farDistance;
    }

    public enum CompassMarkerType : byte {
        [UnityEngine.Scripting.Preserve] Default = 0,
        [UnityEngine.Scripting.Preserve] MediumPriorityLocation = 1,
        [UnityEngine.Scripting.Preserve] HighPriorityLocation = 2,
        [UnityEngine.Scripting.Preserve] AI = 3,
        [UnityEngine.Scripting.Preserve] CombatAI = 4,
    }
}