using System;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/HUD: Compass Hide|Show")]
    public class SEditorChangeCompassVisibility : EditorStep {
        public string id;
        public bool visible;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SChangeCompassVisibility {
                id = id,
                visible = visible
            };
        }
    }

    public partial class SChangeCompassVisibility : StoryStep {
        public string id;
        public bool visible;
        
        public override StepResult Execute(Story story) {
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException("You need to specify ID of the compass visibility change");
            }

            if (visible) {
                World.All<HideCompass>().FirstOrDefault(c => c.SourceID == id)?.Discard();
            } else {
                if (World.All<HideCompass>().All(c => c.SourceID != id)) {
                    World.Add(new HideCompass(id));
                }
            }

            return StepResult.Immediate;
        }
    }
}