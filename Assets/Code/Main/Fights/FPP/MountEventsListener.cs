using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using JetBrains.Annotations;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Fights.FPP {
    [NoPrefab]
    public class VMountEventsListener : View<MountElement> {
        ScriptMachine[] _scriptMachines;
        
        protected override void OnInitialize() {
            _scriptMachines = GetComponents<ScriptMachine>();
        }
        
        // --- Called from animator event
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(Object obj) {
            if (obj is ARAnimationEvent animationEvent) {
                foreach (var machine in _scriptMachines) {
                    machine.TriggerUnityEvent(animationEvent.actionType.ToString());
                }

                foreach (AliveAudioType characterAudio in animationEvent.AliveAudio) {
                    bool asOneShot = characterAudio == AliveAudioType.FootStep;
                    Target.PlayAudioClip(characterAudio, asOneShot);
                }
            }
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void PlayClip(Object obj) {
            if (obj is FModEventRef template) {
                FMODManager.PlayAttachedOneShotWithParameters(template, gameObject, this);
            }
        }

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void Neigh() {
            Target.View<VMount>().Neigh();
        }
    }
}