using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public class NpcGenderMarker : MonoBehaviour {
        [SerializeField] Gender gender;

        public Gender Gender => gender;
    }
    
    public enum Gender {
        None,
        Male,
        Female
    }
}