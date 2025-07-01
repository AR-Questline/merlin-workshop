using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Setup {
    public class CharacterGroundedData : ScriptableObject {
        [FoldoutGroup("Slopes")] public float slopeCriticalAngle = 55f;
        [FoldoutGroup("Slopes")] [UnityEngine.Scripting.Preserve] public float waterSlopeCriticalAngle = 80f;
        [FoldoutGroup("Slopes")] public float slopeFriction = 0.3f;
        
        [FoldoutGroup("Grounding")] public float groundedOffset = -0.14f;
        [FoldoutGroup("Grounding")] public float groundedRadius = 0.3f;
        [FoldoutGroup("Grounding")] public float groundTouchedTimeout = 0.5f;
    }
}