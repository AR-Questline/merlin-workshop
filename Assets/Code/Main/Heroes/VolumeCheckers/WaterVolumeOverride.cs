using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    [RequireComponent(typeof(WaterSurface))]
    public class WaterVolumeOverride : MonoBehaviour {
        [Required]
        public VolumeProfile profile;
        
        [Button]
        void GetProfileFromChild() {
            profile = GetComponentInChildren<Volume>().sharedProfile;
        }
    }
}