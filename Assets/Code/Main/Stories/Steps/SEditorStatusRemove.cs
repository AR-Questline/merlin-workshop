using System;
using System.Linq;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Hero: Remove status")]
    public class SEditorStatusRemove : EditorStep {
        [TemplateType(typeof(StatusTemplate))]
        public TemplateReference[] byTemplate = Array.Empty<TemplateReference>();

        [RichEnumExtends(typeof(StatusType))]
        public RichEnumReference[] byType = Array.Empty<RichEnumReference>();

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStatusRemove {
                byTemplate = byTemplate,
                byType = byType
            };
        }
    }

    public partial class SStatusRemove : StoryStep {
        public TemplateReference[] byTemplate = Array.Empty<TemplateReference>();
        public RichEnumReference[] byType = Array.Empty<RichEnumReference>();
        
        public override StepResult Execute(Story story) {
            var statuses = story.Hero.Statuses;

            foreach (var status in byTemplate.Select(t => t.Get<StatusTemplate>())) {
                statuses.RemoveAllStatus(status);
            }

            foreach (var statusType in byType.Select(t => t.EnumAs<StatusType>())) {
                statuses.RemoveAllStatusesOfType(statusType);
            }

            return StepResult.Immediate;
        }
    }
}