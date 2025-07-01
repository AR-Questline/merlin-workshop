using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    public interface IEditorStep {
        public IEditorChapter ContinuationChapter { get; }
        bool MayHaveContinuation { get; }
    }
}