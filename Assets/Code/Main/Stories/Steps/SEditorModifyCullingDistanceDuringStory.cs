using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Culling Distance: Modify")]
    public class SEditorModifyCullingDistanceDuringStory : EditorStep {
        public bool removeMultiplier;
        [HideIf(nameof(removeMultiplier))] public float multiplierValue = 1f;
        [HideIf(nameof(removeMultiplier))] public bool allowClampOfValues = false;


        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SModifyCullingDistanceDuringStory {
                removeMultiplier = removeMultiplier,
                multiplierValue = multiplierValue,
                allowClampOfValues = allowClampOfValues
            };
        }
    }

    public partial class SModifyCullingDistanceDuringStory : StoryStep {
        public bool removeMultiplier;
        public float multiplierValue = 1f;
        public bool allowClampOfValues;
        
        public override StepResult Execute(Story story) {
            if (removeMultiplier) {
                StoryBasedCullingDistanceMultiplier.Remove(story);
            } else {
                StoryBasedCullingDistanceMultiplier.Create(story, multiplierValue, allowClampOfValues);
            }

            return StepResult.Immediate;
        }
    }

}