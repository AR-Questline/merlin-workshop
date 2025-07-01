using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI {
    [CreateAssetMenu(fileName = "Alert", menuName = "NpcData/Alert")]
    public class AlertData : ScriptableObject {
        [SerializeField] bool canEnterAlert = true;
        [SerializeField, ShowIf(nameof(canEnterAlert))] bool canLookAt = true;
        [SerializeField, ShowIf(nameof(canEnterAlert))] bool canWander = true;
        [SerializeField, Tooltip("Radius that AI search for target in when it is in AlertWander")] float searchRadius;
        [SerializeField] float visionAlertGainMultiplier = 1;
        
        public bool CanEnterAlert => canEnterAlert;
        public bool CanLookAt => canLookAt;
        public bool CanWander => canWander;
        public float SearchRadius => searchRadius;
        public float VisionAlertGainMultiplier => visionAlertGainMultiplier;
    }
}