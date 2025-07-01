using Animancer;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Fishing {
    [RequireComponent(typeof(SphereCollider))]
    public class FishVolume : MonoBehaviour, IFishVolume {
        [Range(0.1f, 1f)] const float DensityInterval = 0.25f;

        public const int Layer = RenderLayers.TriggerVolumes;
        public const int Mask = 1 << Layer;

        [SerializeField] FishTable fish;
        [SerializeField, Tooltip("How much fish per 10 seconds")] float density = 1;

        public bool IsCorrectlySetUp => !fish.entries.IsNullOrEmpty();
        public FishTable AllFish => fish;
        
        SphereCollider _collider;

        void Awake() {
            gameObject.layer = Layer;
            _collider = GetComponent<SphereCollider>();
            _collider.isTrigger = true;
        }
        
        public virtual void OnGetVolume() { }

        public float GetDensity(Vector3 position) {
            const float FullDensityPercent = 0.3f;
            const float DensityParamA = 1 / (FullDensityPercent - 1);
            
            float distanceSq = position.SquaredDistanceTo(transform.TransformPoint(_collider.center));
            float radiusSq = _collider.radius.Squared();

            float densityProfile = (DensityParamA * (distanceSq / radiusSq) - DensityParamA);
            return densityProfile * density / DensityInterval;
        }
        
        public ref readonly FishData FishData() {
            if (IsCorrectlySetUp) {
                return ref GetFish();
            }

            return ref GenericFishVolume.Instance.FishData();
        }
        
        ref readonly FishData GetFish() {
            return ref fish.GetRandomFish();
        }

        public void DebugSetHugeDensity() {
            density = 1000f;
        }
        
#if UNITY_EDITOR
        void Reset() {
            gameObject.layer = Layer;
            var coll = GetComponent<SphereCollider>();
            if (coll != null) {
                coll.isTrigger = true;
            }
        }
#endif
    }
}