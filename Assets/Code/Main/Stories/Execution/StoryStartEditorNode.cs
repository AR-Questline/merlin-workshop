using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Extensions;
using Awaken.TG.Main.Stories.Steps;
using XNode;

namespace Awaken.TG.Main.Stories.Execution {
    [NodeTint(0.5f, 0.7f, 0.5f)]
    [NodeWidth(260)]
    [CreateNodeMenu("", typeof(StoryGraph))]
    public class StoryStartEditorNode : StoryNode<SEditorStoryStartChoice>, IEditorChapter, IStorySettings {
        [Output(connectionType = ConnectionType.Override)]
        public Node chapter;
        [Input] public ChapterEditorNode[] link = Array.Empty<ChapterEditorNode>();
        public bool involveHero = true;
        public bool involveAI = true;
        public bool enableChoices;

        public bool InvolveHero => involveHero;
        public bool InvolveAI => involveAI;
        
        public ChapterEditorNode Chapter => GetPort((NodePort.FieldNameCompressed)nameof(chapter)).ConnectedNode() as ChapterEditorNode;
        public IEnumerable<IEditorStep> Steps {
            get {
                foreach (var choice in Elements) {
                    yield return choice;
                }
                yield return new SEditorLeave();
            }
        }
        public IEditorChapter ContinuationChapter => enableChoices ? null : Chapter;
        public bool IsEmptyAndHasNoContinuation => false;

        void Reset() {
            name = "Story Start";
        }

        public override object GetValue(NodePort port) {
            return this;
        }
    }
}