using Awaken.TG.Main.Stories.Choices;
using Awaken.TG.Main.Stories.Debugging;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Collections;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Runtime.Nodes {
    public abstract class StoryStep {
        public StoryConditionInput[] conditions;
        public StoryChapter parentChapter;

#if UNITY_EDITOR
        [CanBeNull] public DebugInfo DebugInfo { get; set; }
#endif

        public bool AutoPerformed { get; protected init; } = true;
        
        public virtual StepRequirement GetRequirement() => _ => true;

        public abstract StepResult Execute(Story story);
        
        public virtual void AppendKnownEffects(Story story, ref StructList<string> effects) { }
        public virtual void AppendHoverInfo(Story story, ref StructList<IHoverInfo> hoverInfo) { }
        public virtual string GetKind(Story story) => null;

        public abstract byte Type { get; }
        
        public virtual void Write(StoryWriter serializer) {
            serializer.Write(conditions);
        }

        public virtual void Read(StoryReader serializer) {
            serializer.Read(ref conditions);
        }
        
        public bool IsLastStep() {
            return parentChapter.steps[^1] is SLeave && parentChapter.steps[^2] == this;
        }
    }
}