using System;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Stories.Steps {
    [Serializable]
    public abstract class EditorStep : NodeElement<ChapterEditorNode>, IEditorStep {
        public IEditorChapter ContinuationChapter => Parent.ContinuationChapter;
        public virtual bool MayHaveContinuation => false;

        public StoryStep CreateRuntimeStep(StoryGraphParser parser) {
            var conditions = new StructList<StoryConditionInput>(0);
            foreach (var condition in ConditionNodes()) {
                var inputConditions = parser.GetConditions(condition);
                if (inputConditions != null) {
                    conditions.Add(new StoryConditionInput {
                        conditions = inputConditions,
                        negate = condition.IsConnectionNegated(this),
                    });
                }
            }
            
            var step = CreateRuntimeStepImpl(parser);
            if (step != null) {
                step.conditions = conditions.ToArray();
#if UNITY_EDITOR
                step.DebugInfo = DebugInfo;
#endif
            }
            return step;
        }
        protected abstract StoryStep CreateRuntimeStepImpl(StoryGraphParser parser);
    }

    /// <summary>
    /// For hard requirements. If any step has a hard requirement and it is not fulfilled, it's not
    /// possible to take the choice that would lead to a step. For example, if something costs 20$,
    /// having 20$ is a hard requirement for that step.
    /// </summary>
    public delegate bool StepRequirement(Story story);
}