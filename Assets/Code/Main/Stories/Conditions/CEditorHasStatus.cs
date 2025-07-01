using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if hero has status
    /// </summary>
    [Element("Hero: Has Status")]
    public class CEditorHasStatus : EditorCondition {

        [TemplateType(typeof(StatusTemplate))]
        public TemplateReference requiredStatus;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CHasStatus {
                requiredStatus = requiredStatus
            };
        }
    }

    public partial class CHasStatus : StoryCondition {
        public TemplateReference requiredStatus;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            CharacterStatuses statuses = Hero.Current?.Statuses;
            if (statuses == null) {
                return false;
            }
            if (requiredStatus == null || !requiredStatus.IsSet) {
                return true;
            }
            return statuses.HasStatus(requiredStatus.Get<StatusTemplate>());
        }
    }
}