using UnityEngine.Rendering;

namespace Awaken.Utility.Graphics {
    public static class VolumeExtension {
        public static VolumeProfile GetSharedOrInstancedProfile(this Volume volume) {
            return volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
        }
        
        public static void SetSharedOrInstancedProfile(this Volume volume, VolumeProfile profile) {
            if (volume.HasInstantiatedProfile()) {
                volume.profile = profile;
            } else {
                volume.sharedProfile = profile;
            }
        }
        
        public static bool TryGetVolumeComponent<T>(this Volume volume, out T component) where T : VolumeComponent {
            component = default;
            if (!volume) {
                return false;
            }
            var profile = volume.GetSharedOrInstancedProfile();
            return profile != null && profile.TryGet(out component);
        }
    }
}
