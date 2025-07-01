using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets {
    public class VCPetEventsListener : ViewComponent<Location> {
        PetElement _pet;
        
        protected override void OnAttach() {
            Target.AfterFullyInitialized(OnLocationFullyInitialized);
        }

        void OnLocationFullyInitialized() {
            if (!Target.TryGetElement(out _pet)) {
                Log.Important?.Error($"{nameof(VCPetEventsListener)} found in a non-pet location!");
            }
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(Object obj) {
            if (_pet == null) {
                return;
            }
            
            if (obj is ARAnimationEvent animationEvent) {
                foreach (AliveAudioType characterAudio in animationEvent.AliveAudio) {
                    bool asOneShot = characterAudio == AliveAudioType.FootStep;
                    _pet.PlayAudioClip(characterAudio, asOneShot);
                }
            }
        }
        
        protected override void OnDiscard() {
            _pet = null;
        }
    }
}