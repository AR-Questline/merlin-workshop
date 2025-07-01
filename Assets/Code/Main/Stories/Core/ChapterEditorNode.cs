using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Extensions;
using Awaken.TG.Main.Stories.Steps;
using XNode;

namespace Awaken.TG.Main.Stories.Core {
    /// <summary>
    /// Basic node used for running steps from top to bottom.
    /// Has continuation link, which allows connecting chapters without explicit jumps/choices.
    /// </summary>
    [NodeWidth(300)]
    [Serializable]
    public class ChapterEditorNode : StoryNode<EditorStep>, IEditorChapter {
        [Output(connectionType = ConnectionType.Override)] public ChapterEditorNode continuation;
        [Input] public ChapterEditorNode[] link = Array.Empty<ChapterEditorNode>();

        public IEditorChapter ContinuationChapter => GetOutputPort((NodePort.FieldNameCompressed)nameof(continuation)).ConnectedNode() as IEditorChapter;
        public IEnumerable<IEditorStep> Steps => ScanSteps();
        public bool IsEmptyAndHasNoContinuation => !StepsFrom(this).Any() && ContinuationChapter == null;

        // === Helper

        public T GetStep<T>() where T : IEditorStep {
            return ScanSteps().OfType<T>().FirstOrDefault();
        }

        public IEnumerable<T> GetSteps<T>() {
            return ScanSteps().OfType<T>();
        }

        public IEnumerable<IEditorStep> ScanSteps() {
            bool endNeeded = true;
            IEditorStep lastStep = null;

            HashSet<IEditorStep> visitedSteps = new HashSet<IEditorStep>();
            foreach (IEditorStep step in StepsFrom(this)) {
                if (!visitedSteps.Add(step)) {
                    yield break;
                }

                endNeeded = endNeeded && !step.MayHaveContinuation;
                lastStep = step;
                yield return step;
            }

            endNeeded = endNeeded && (lastStep?.ContinuationChapter == null || lastStep.ContinuationChapter.IsEmptyAndHasNoContinuation);
            if (endNeeded) {
                yield return new SEditorLeave();
            }
        }

        protected static IEnumerable<IEditorStep> StepsFrom(IEditorChapter chapter) {
            // leave if empty
            if (chapter == null) {
                yield break;
            }

            // directly contained components
            if (chapter is StoryStartEditorNode storyStart) {
                foreach (var step in storyStart.Steps) {
                    yield return step;
                }
            } else if (chapter is ChapterEditorNode chap) {
                foreach (EditorStep step in chap.Elements) {
                    yield return step;
                }
            }

            // components from children
            foreach (IEditorStep step in StepsFrom(chapter.ContinuationChapter)) {
                yield return step;
            }
        }
    }
}