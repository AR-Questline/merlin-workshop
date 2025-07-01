using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Utility.Threads;
using FMODUnity;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.AI.Fights.Utils {
    public class HeroAnimatorClipPlayer : MonoBehaviour {
        public void PlayClipDirect(string pathToEvent) {
            // RuntimeManager.PlayOneShot(pathToEvent);
        }

        public void PlayClip(Object o) {
            MainThreadDispatcher.InvokeAsync(() => {
                if (o is FModEventRef template) {
                    FMODManager.PlayAttachedOneShotWithParameters(template, gameObject, this);
                }
            });
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void ToggleArrowInMainHand(int enabled) {
            HeroWeaponEvents.Current.ToggleArrowInMainHand(enabled);
        }
    }
}
