using System;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Conditions {
    [Serializable]
    public class ModifiedStatConditionPart : BasePart {
        [UnityEngine.Scripting.Preserve] public float value;
        [UnityEngine.Scripting.Preserve] public Comparison comparison;
        
        public override UniTask<bool> OnRun(TutorialContext context) {
            // var statDisplay = context.vc.ParentView as VModifiedStatDisplay;
            // if (statDisplay == null) {
            //     return UniTask.FromResult(false);
            // }
            //
            // bool satisfied = value.CompareTo(statDisplay.Stat.ModifiedInt) == (int) comparison;
            return UniTask.FromResult(false);
        }
    }
}