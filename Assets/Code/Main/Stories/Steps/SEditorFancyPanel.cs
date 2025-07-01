using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Fancy: Open Panel")]
    public class SEditorFancyPanel : EditorStep {
        [TextArea(2, 20), LocStringCategory(Category.UI)]
        public LocString text;
        [RichEnumExtends(typeof(FancyPanelType))]
        public RichEnumReference type;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SFancyPanel {
                text = text,
                type = type
            };
        }
    }

    public partial class SFancyPanel : StoryStep {
        public LocString text;
        public RichEnumReference type;
        
        public override StepResult Execute(Story story) {
            type.EnumAs<FancyPanelType>().Spawn(story, text);
            return StepResult.Immediate;
        }
    }
}