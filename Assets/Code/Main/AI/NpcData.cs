using UnityEngine;

namespace Awaken.TG.Main.AI {
    [CreateAssetMenu(fileName = "NpcData", menuName = "NpcData/Main")]
    public class NpcData : ScriptableObject {
        public PerceptionData perception;
        public AlertData alert;
    }
}