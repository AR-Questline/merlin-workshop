using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Utility.Threads;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Utils {
    public class SimpleAnimatorClipPlayer : MonoBehaviour {
        public void PlayClip(Object o) {
            MainThreadDispatcher.InvokeAsync(() => {
                if (o is FModEventRef template) {
                    FMODManager.PlayAttachedOneShotWithParameters(template, gameObject, this);
                }
            });
        }
    }
}