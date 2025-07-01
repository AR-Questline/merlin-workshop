using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class SetActivePart : BasePart {
        public GameObject target;
        
        public override UniTask<bool> OnRun(TutorialContext context) {
            target.SetActive(true);
            context.onFinish += () => {
                if (target != null) {
                    target.SetActive(false);
                }
            };
            return UniTask.FromResult(true);
        }
        
        public override void TestRun(TutorialContext context) {
            target.SetActive(true);
            context.onFinish += () => target.SetActive(false);
        }
    }
}