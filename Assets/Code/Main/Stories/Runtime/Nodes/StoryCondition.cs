using Awaken.TG.Main.Stories.Debugging;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Runtime.Nodes {
    public abstract class StoryCondition {
        public abstract bool Fulfilled([CanBeNull] Story story, StoryStep step);

#if UNITY_EDITOR
        [CanBeNull] public DebugInfo DebugInfo { get; set; }
#endif
        
        public abstract byte Type { get; }
        public virtual void Write(StoryWriter writer) { }
        public virtual void Read(StoryReader reader) { }
    }
}