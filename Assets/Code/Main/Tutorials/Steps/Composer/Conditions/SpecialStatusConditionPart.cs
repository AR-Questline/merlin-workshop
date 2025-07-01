using System;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Templates;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Conditions {
    [Serializable]
    public class SpecialStatusConditionPart : BasePart {
        [TemplateType(typeof(StatusTemplate))]
        public TemplateReference requiredStatusRef;

        StatusTemplate RequiredTemplate => requiredStatusRef.Get<StatusTemplate>();
        
        public override UniTask<bool> OnRun(TutorialContext context) {
            if (context.target is Status status && status.Template == RequiredTemplate) {
                return UniTask.FromResult(true);
            }

            return UniTask.FromResult(false);
        }
    }
}