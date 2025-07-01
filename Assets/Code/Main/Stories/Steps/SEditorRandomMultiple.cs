using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps
{
    /// <summary>
    /// Picks randomly between several possible chapters.
    /// </summary>
    [Element("Branch/Branch: Random Multiple")]
    public class SEditorRandomMultiple : EditorStep {
        [Tooltip("This step picks given number of options from all. Options are defined by \"Branch: Random\" steps.")]
        public int pickCount;

        public override bool MayHaveContinuation => true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SRandomMultiple {
                pickCount = pickCount
            };
        }
    }

    public partial class SRandomMultiple : StoryStep {
        public int pickCount;
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            ExecuteRandom(story, result).Forget();
            return result;
        }

        async UniTaskVoid ExecuteRandom(Story story, StepResult mainResult) {
            var validPicks = SRandomPick.ValidPicks(story, parentChapter);
            int picked = 0;

            while (validPicks.Count > 0 && picked < pickCount) {
                var pick = RandomUtil.WeightedSelect(validPicks, i => i.weight);
                var result = pick.PerformJump(story);
                picked++;
                validPicks.Remove(pick);
                while (!result.IsDone) {
                    await UniTask.NextFrame();
                }
            }
            
            mainResult.Complete();
        }
    }
}
