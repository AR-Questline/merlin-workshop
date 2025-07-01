using Awaken.TG.Main.Stories.Core;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Runtime.Nodes {
    public class StoryChapter {
        public StoryStep[] steps;
        public StoryChapter continuation;
        
#if UNITY_EDITOR
        [CanBeNull] public ChapterEditorNode EditorNode { get; set; }
#endif
    }
}