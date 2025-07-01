using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Universal;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class EffectPart : BasePart {
        public GameObject uiEffect;
        public Transform uiEffectParent;

        public override UniTask<bool> OnRun(TutorialContext context) {
            var effect = World.SpawnViewFromPrefab<VUIEffect>(context.target, uiEffect, false, true, uiEffectParent != null ? uiEffectParent : context.vc.transform);
            context.onFinish += effect.Discard;
            return UniTask.FromResult(true);
        }
    }
}