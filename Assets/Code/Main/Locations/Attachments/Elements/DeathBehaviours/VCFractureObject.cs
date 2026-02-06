using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class VCFractureObject : ViewComponent<Location> {
        const float ShrinkDelay = 2f;
        const float ShrinkingTime = 1f;

        [FoldoutGroup("Prefabs"), SerializeField] GameObject originalObject;
        [FoldoutGroup("Prefabs"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, true, AddressableGroup.Locations)]
        ARAssetReference fracturedObjectReference;
        [FoldoutGroup("Prefabs"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, true, AddressableGroup.Locations)]
        ARAssetReference vfxObjectReference;
        [FoldoutGroup("Force"), SerializeField] float explosionForceMin;
        [FoldoutGroup("Force"), SerializeField] float explosionForceMax;
        [FoldoutGroup("Force"), SerializeField] float explosionForceRadius;
        [FoldoutGroup("Force"), SerializeField] float upwardsModifier = 3f;
        [SerializeField] bool fractureOnDeath = true;
        [SerializeField] bool discardAfterFracture = true;
        ARAsyncOperationHandle<GameObject> _fractureHandle;
        ARAsyncOperationHandle<GameObject> _vfxHandle;
        GameObject _fractureResult;
        GameObject _vfxResult;
        int _fractureCount;
        
        protected override void OnAttach() {
            Target.OnVisualLoaded(AttachCallbacks);
        }

        void AttachCallbacks(Transform transform) {
            if (Target.TryGetElement<IAlive>(out var alive)) {
                if (fractureOnDeath) {
                    alive.ListenTo(IAlive.Events.BeforeDeath, Fracture, this);
                }
                alive.ListenTo(IAlive.Events.Fracture, Fracture, this);
                alive.ListenTo(IAlive.Events.ResetFracture, ResetFracture, this);
            } else {
                Log.Important?.Error("VCFractureObject should be attached to a location with AliveLocation element!");
            }
        }

        protected override void OnDiscard() {
            ReleaseAssets();
        }

        void ReleaseAssets() {
            if (_fractureHandle.IsValid()) {
                _fractureResult = null;
                _fractureHandle = default;
                fracturedObjectReference.ReleaseAsset();
            }
            if (_vfxHandle.IsValid()) {
                _vfxResult = null;
                _vfxHandle = default;
                vfxObjectReference.ReleaseAsset();
            }
        }
        
        [Button("Fracture")]
        void Fracture() {
            if (originalObject == null || !fracturedObjectReference.IsSet) {
                Log.Important?.Error("VCFractureObject is missing originalObject or fracturedObjectReference!", gameObject);
                return;
            }

            if (fracturedObjectReference.IsSet) {
                if (_fractureHandle.IsValid() == false) {
                    _fractureHandle = fracturedObjectReference.LoadAsset<GameObject>();
                    _fractureHandle.OnComplete(InitFracturedObject);
                } else if (_fractureResult != null) {
                    InstantiateFracturedObject();
                }
            }

            if (vfxObjectReference.IsSet) {
                if (_vfxHandle.IsValid() == false) {
                    _vfxHandle = vfxObjectReference.LoadAsset<GameObject>();
                    _vfxHandle.OnComplete(InitVFXObject);
                } else if (_vfxResult != null) {
                    InstantiateVFXObject();
                }
            }
        }

        void ResetFracture(bool value) {
            originalObject.SetActive(value);
        }

        void InitFracturedObject(ARAsyncOperationHandle<GameObject> fragObj) {
            _fractureResult = fragObj.Result;
            InstantiateFracturedObject();
        }

        void InstantiateFracturedObject() {
            _fractureCount++;
            originalObject.SetActive(false);
            
            var position = originalObject.transform.position;
            var fragTransform = Instantiate(_fractureResult, position, originalObject.transform.rotation, null).transform;
            
            Transform[] children = new Transform[fragTransform.childCount];
            for (var i = 0; i < fragTransform.childCount; i++) {
                children[i] = fragTransform.GetChild(i);
                children[i].GetComponent<Rigidbody>()?.AddExplosionForce(
                    Random.Range(explosionForceMin, explosionForceMax),
                    position, explosionForceRadius, upwardsModifier);
            }
            
            Shrink(children, fragTransform.gameObject).Forget();
        }
        
        void InitVFXObject(ARAsyncOperationHandle<GameObject> vfxObj) {
            _vfxResult = vfxObj.Result;
            InstantiateVFXObject();
        }

        void InstantiateVFXObject() {
            var position = originalObject.transform.position;
            var vfxObject = Instantiate(_vfxResult, position, originalObject.transform.rotation, null);
            Destroy(vfxObject, ShrinkDelay + ShrinkingTime);
        }

        async UniTaskVoid Shrink(Transform[] childTransforms, GameObject parent) {
            var successful = await AsyncUtil.DelayTime(this, ShrinkDelay);
            if (!successful) {
                return;
            }
            
            Vector3 scale = childTransforms[0].localScale;
            Vector3 scaleLose = scale / ShrinkingTime;

            while (scale.x >= 0) {
                scale -= scaleLose * Time.deltaTime;
                foreach (var t in childTransforms) {
                    t.localScale = scale;
                }
                
                successful = await AsyncUtil.DelayFrame(this);
                if (!successful) {
                    return;
                }
            }

            Destroy(parent);
            if (discardAfterFracture) {
                Target.Discard();
            } else {
                _fractureCount--;
                if (_fractureCount == 0) {
                    ReleaseAssets();
                }
            }
        }
    }
}
