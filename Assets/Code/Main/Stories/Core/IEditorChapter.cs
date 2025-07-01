using System.Collections.Generic;
using Awaken.TG.Main.Stories.Steps;

namespace Awaken.TG.Main.Stories.Core {
    public interface IEditorChapter {
        IEnumerable<IEditorStep> Steps { get; }
        IEditorChapter ContinuationChapter { get; }
        bool IsEmptyAndHasNoContinuation { get; }
    }
}
