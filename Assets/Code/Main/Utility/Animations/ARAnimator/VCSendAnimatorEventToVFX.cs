using System;
using Awaken.TG.MVC;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    public class VCSendAnimatorEventToVFX : ViewComponent<IModel> {
        [SerializeField] VisualEffect[] visualEffects = Array.Empty<VisualEffect>();
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void FootStep() {
            foreach (var vfx in visualEffects) {
                vfx.SendEvent("FootStep");
            }
        }
    }
}