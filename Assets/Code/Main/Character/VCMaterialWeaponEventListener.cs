using Awaken.Kandra;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public class VCMaterialWeaponEventListener : ViewComponent<Location>, IMaterialWeaponEventListenerProvider {
        [SerializeField] ARAnimationEvent.ActionType activateEvent;
        [SerializeField] ARAnimationEvent.ActionType deactivateEvent;
        [SerializeField] KandraRenderer rendererWithMaterial;
        [SerializeField] int materialIndex;
        [SerializeField] string parameter;
        [SerializeField] float valueActivated;
        [SerializeField] float valueDeactivated;
        [SerializeField] float lerpTime;
        
        int _lastAnimationEventFrame;
        Object _lastAnimationEventObject;
        MaterialWeaponEventListener<VCMaterialWeaponEventListener> _listener;
        
        public int MaterialIndex => materialIndex;
        public string Parameter => parameter;
        public float ValueActivated => valueActivated;
        public float ValueDeactivated => valueDeactivated;
        public float LerpTime => lerpTime;

        public MaterialWeaponEventListener<VCMaterialWeaponEventListener> Listener => _listener ??= new (this, Target.TryGetElement<IAlive>(), rendererWithMaterial);

        protected override void OnAttach() { }
        
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

        void Activate() {
            Listener.Activate();
        }

        void Deactivate() {
            Listener.Deactivate();
        }
        
        protected override void OnDiscard() {
            _listener?.OnDiscard();
            _listener = null;
        }
    }
}
