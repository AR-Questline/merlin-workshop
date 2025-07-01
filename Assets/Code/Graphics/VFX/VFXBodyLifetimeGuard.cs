using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    public class VFXBodyLifetimeGuard : MonoBehaviour {
        static readonly List<VFXBodyLifetimeGuard> ReusableGuards = new();
        
        [SerializeField] VFXBodyMarker marker;
        [SerializeField] DisableStrategy onDisable;
        
        void OnEnable() {
            if (marker) {
                marker.MarkBeingUsed();
            }
        }

        void OnDisable() {
            if (marker) {
                marker.MarkBeingUnused();
            }
            if (onDisable == DisableStrategy.Destroy) {
                marker = null;
                Destroy(this);
            } else if (onDisable == DisableStrategy.Clear) {
                marker = null;
            }
        }

        public void SetMarker(VFXBodyMarker newMarker, bool markBeingUsed) {
            if (enabled && marker) {
                marker.MarkBeingUnused();
            }
            marker = newMarker;
            if (markBeingUsed && enabled && marker) {
                marker.MarkBeingUsed();
            } 
        }

        public void ResetMarker() {
            if (enabled && marker) {
                marker.MarkBeingUnused();
            }
            marker = null;
        }
        
        public static void Add(GameObject go, VFXBodyMarker marker, bool markBeingUsed) {
            if (marker == null) {
                return;
            }
            go.GetComponents(ReusableGuards);
            var guard = GetGuardToSet(go, ReusableGuards);
            guard.SetMarker(marker, markBeingUsed);
            ReusableGuards.Clear();

            static VFXBodyLifetimeGuard GetGuardToSet(GameObject go, List<VFXBodyLifetimeGuard> existingGuards) {
                foreach (var guard in existingGuards) {
                    if (guard.marker == null && guard.onDisable == DisableStrategy.Clear) {
                        return guard;
                    }
                }
                var newGuard = go.AddComponent<VFXBodyLifetimeGuard>();
                newGuard.onDisable = DisableStrategy.Clear;
                return newGuard;
            }
        }

        enum DisableStrategy : byte {
            [UnityEngine.Scripting.Preserve] None,
            Clear,
            Destroy,
        }
    }
}