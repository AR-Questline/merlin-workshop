using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Awaken.TG.Graphics.Saturation {
    [CreateAssetMenu]
    public class SaturationReferences : ScriptableObject {
        [Preserve]
        [SerializeField, EnableIf(nameof(CanEdit))] DiffusionProfileSettings[] profiles = Array.Empty<DiffusionProfileSettings>();

        ProfileCache[] _defaultProfiles;

        public void Init() {
            _defaultProfiles = CacheProfiles(profiles);
        }
        
        public void OnSaturationChanged(float saturation) {
            if (_defaultProfiles == null) { // if editing
                return;
            }
            for (int i = 0; i < profiles.Length; i++) {
                var profile = profiles[i];
                if (profile == null) {
                    continue;
                }
                ref readonly var cache = ref _defaultProfiles[i];
                profile.scatteringDistance = SaturationStack.Saturate(cache.scatteringDistance, saturation);
                profile.transmissionTint = SaturationStack.Saturate(cache.transmissionTint, saturation);
            }
        }

        [Button, ShowIf(nameof(CanEdit))] 
        void StopEditing() {
            _defaultProfiles = CacheProfiles(profiles);
            OnSaturationChanged(SaturationStack.Instance.Saturation);
        }
        
        [Button, HideIf(nameof(CanEdit))]
        void StartEditing() {
            OnSaturationChanged(1);
            _defaultProfiles = null;
        }

        bool CanEdit() {
            return _defaultProfiles == null;
        }
        
        static ProfileCache[] CacheProfiles(DiffusionProfileSettings[] profiles) {
            var cached = new ProfileCache[profiles.Length];
            for (int i = 0; i < profiles.Length; i++) {
                cached[i] = new ProfileCache(profiles[i]);
            }
            return cached;
        }
        
        readonly struct ProfileCache {
            public readonly Color scatteringDistance;
            public readonly Color transmissionTint;

            public ProfileCache(DiffusionProfileSettings profile) {
                scatteringDistance = profile.scatteringDistance;
                transmissionTint = profile.transmissionTint;
            }
        }
    }
}