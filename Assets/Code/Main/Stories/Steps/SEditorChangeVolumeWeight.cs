using System;
using Awaken.TG.Graphics.DayNightSystem;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Volume: Manually Change State")]
    public class SEditorChangeVolumeWeight : EditorStep {
        [Tags(TagsCategory.Location)] public string[] tags = Array.Empty<string>();
        public int state;
        public bool instant;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SChangeVolumeWeight {
                tags = tags,
                state = state,
                instant = instant,
            };
        }
    }
    
    public partial class SChangeVolumeWeight : StoryStep {
        public string[] tags;
        public int state;
        public bool instant;

        public override StepResult Execute(Story story) {
            foreach (var controller in ManualVolumeController.GetControllersWithTags(tags)) {
                if (instant) {
                    controller.ChangeStateInstant(state);
                } else {
                    controller.ChangeState(state);
                }
            }
            return StepResult.Immediate;
        }

    }
}