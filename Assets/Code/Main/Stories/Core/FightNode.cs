using System;
using Awaken.TG.Main.Stories.Extensions;
using XNode;

namespace Awaken.TG.Main.Stories.Core {
    [NodeWidth(300)]
    [Serializable]
    [NodeTint(0.4f, 0.4f, 0.2f)]
    public class FightNode : ChapterEditorNode {
        [Output] public FightNode failure;
        [Output] public FightNode retreat;

        public ChapterEditorNode FailureChapter => GetPort((NodePort.FieldNameCompressed)nameof(failure)).ConnectedNode() as ChapterEditorNode;
        public ChapterEditorNode RetreatChapter => GetPort((NodePort.FieldNameCompressed)nameof(retreat)).ConnectedNode() as ChapterEditorNode;
    }
}