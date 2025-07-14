using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public class NpcGenderMarker : MonoBehaviour {
        [SerializeField] Gender gender;

        public Gender Gender => gender;
    }
    
    public enum Gender : byte {
        None = 0,
        Male = 1,
        Female = 2
    }
}