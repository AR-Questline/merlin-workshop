using System.Collections.Generic;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Character {
    public class VCVfxWeaponEventListener : ViewComponent<Location> {
        [SerializeField] ARAnimationEvent.ActionType activateEvent;
        [SerializeField] ARAnimationEvent.ActionType deactivateEvent;
        [SerializeField] List<VisualEffect> effects;
        [SerializeField] List<GameObject> objectsToEnable;
        
        int _lastAnimationEventFrame;
        Object _lastAnimationEventObject;
        
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
                    ActivateVfxs();
                } else if (animationEvent.actionType == deactivateEvent) {
                    DeactivateVfxs();
                }
            }
        }

        void ActivateVfxs() {
            foreach (var effect in effects) {
                effect.Play();
            }

            foreach (var objectToEnable in objectsToEnable) {
                objectToEnable.SetActive(true);
            }
        }
        
        void DeactivateVfxs() {
            foreach (var effect in effects) {
                VFXUtils.StopVfx(effect);
            }
            
            foreach (var objectToEnable in objectsToEnable) {
                objectToEnable.SetActive(false);
            }
        }
    }
}
