using System.Collections.Generic;
using Awaken.Kandra;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Character {
    public class VCClothMaterialWeaponEventListener : ViewComponent<Location>, IMaterialWeaponEventListenerProvider {
        [SerializeField] ARAnimationEvent.ActionType activateEvent;
        [SerializeField] ARAnimationEvent.ActionType deactivateEvent;
        [SerializeField] int materialIndex;
        [SerializeField] string parameter;
        [SerializeField] float valueActivated;
        [SerializeField] float valueDeactivated;
        [SerializeField] float lerpTime;

        bool _active;
        int _lastAnimationEventFrame;
        Object _lastAnimationEventObject;
        Dictionary<KandraRenderer, MaterialWeaponEventListener<VCClothMaterialWeaponEventListener>> _clothRenderers;
        
        public int MaterialIndex => materialIndex;
        public string Parameter => parameter;
        public float ValueActivated => valueActivated;
        public float ValueDeactivated => valueDeactivated;
        public float LerpTime => lerpTime;

        protected override void OnAttach() {
            var clothes = Target.Character?.Clothes;
            clothes?.ListenTo(BaseClothes.Events.ClothEquipped, OnClothEquipped, this);
            clothes?.ListenTo(BaseClothes.Events.ClothEquipped, OnClothUnequipped, this);
        }

        protected void OnEnable() {
            // ClothToStitch (and probably other systems) add meshes OnEnable, so we need to wait a frame before checking for missing renderers.
            TryToFindMissingRenderersAfterFrame().Forget();
        }

        async UniTaskVoid TryToFindMissingRenderersAfterFrame() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            TryToFindMissingRenderers();
        }

        void TryToFindMissingRenderers() {
            if (this == null || gameObject == null || HasBeenDiscarded) {
                return;
            }
            
            var renderersMarkers = GetComponentInParent<RenderersMarkers>(true);
            var vfxMarker = Target.Character?.VFXBodyMarker;
            
            if (renderersMarkers != null || vfxMarker != null) {
                foreach (var kandraRenderer in GetComponentsInChildren<KandraRenderer>(true)) {
                    bool isAdded = true;
                    if (renderersMarkers) {
                        foreach (var marker in renderersMarkers.KandraMarkers) {
                            if (marker.Renderer == kandraRenderer) {
                                isAdded = false;
                                break;
                            }
                        }
                    }
                    if (isAdded && vfxMarker) {
                        if (vfxMarker.Renderer == kandraRenderer) {
                            isAdded = false;
                        }
                    }
                    if (isAdded) {
                        _clothRenderers ??= new ();
                        OnKandraAdded(kandraRenderer);
                    }
                }
            }
        }
        
        // --- Called from animator event
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(Object obj) {
            if (Target is not { HasBeenDiscarded: false }) {
                return;
            }

            // --- Head animations use the same animation as the weapons, so we need to filter out events from those animations.
            if (_lastAnimationEventFrame == Time.frameCount && obj == _lastAnimationEventObject) {
                return;
            }
            _lastAnimationEventFrame = Time.frameCount;
            _lastAnimationEventObject = obj;

            if (obj is ARAnimationEvent animationEvent) {
                // --- Actions
                if (animationEvent.actionType == activateEvent) {
                    Activate();
                } else if (animationEvent.actionType == deactivateEvent) {
                    Deactivate();
                }
            }
        }
        
        void OnClothEquipped(GameObject cloth) {
            _clothRenderers ??= new ();
            foreach (var kandraRenderer in cloth.GetComponentsInChildren<KandraRenderer>(true)) {
                OnKandraAdded(kandraRenderer);
            }
        }

        void OnClothUnequipped(GameObject cloth) {
            if (_clothRenderers == null) {
                return;
            }
            foreach (var kandraRenderer in cloth.GetComponentsInChildren<KandraRenderer>(true)) {
                OnKandraRemoved(kandraRenderer);
            }
        }

        void OnKandraAdded(KandraRenderer kandraRenderer) {
            if (!_clothRenderers.ContainsKey(kandraRenderer)) {
                var listener = new MaterialWeaponEventListener<VCClothMaterialWeaponEventListener>(this, Target.TryGetElement<IAlive>(), kandraRenderer);
                _clothRenderers[kandraRenderer] = listener;
                if (_active) {
                    listener.Activate();
                }
            }
        }

        void OnKandraRemoved(KandraRenderer kandraRenderer) {
            if (_clothRenderers.TryGetValue(kandraRenderer, out var listener)) {
                listener.OnDiscard();
                _clothRenderers.Remove(kandraRenderer);
            }
        }

        void Activate() {
            if (_active) {
                return;
            }
            _active = true;
            if (_clothRenderers == null) {
                return;
            }
            foreach (var listener in _clothRenderers.Values) {
                listener.Activate();
            }
        }

        void Deactivate() {
            if (!_active) {
                return;
            }
            _active = false;
            if (_clothRenderers == null) {
                return;
            }
            foreach (var listener in _clothRenderers.Values) {
                listener.Deactivate();
            }
        }

        protected override void OnDiscard() {
            if (_clothRenderers == null) {
                return;
            }
            foreach (var listener in _clothRenderers.Values) {
                listener.OnDiscard();
            }
            _clothRenderers = null;
        }
    }
}
