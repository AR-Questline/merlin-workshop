using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Dialogue/Hero Camera: Change damping")]
    public class SEditorHeroCameraChangeDamping : EditorStep {
        [Min(0f)] public float damping = 4f;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SHeroCameraChangeDamping {
                damping = damping
            };
        }
    }

    public partial class SHeroCameraChangeDamping : StoryStep {
        public float damping = 4f;
        
        public override StepResult Execute(Story story) {
            var dampingOverride = story.TryGetElement<HeroCameraDampingOverride>();
            if (dampingOverride != null) {
                dampingOverride.SetDamping(damping);
            } else {
                story.AddElement(new HeroCameraDampingOverride(damping));
            }
            return StepResult.Immediate;
        }
    }
}
