using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Stories.Runtime {
    /// <summary>
    /// Responsible for running steps from given steps container.
    /// </summary>
    public class StepSequenceRunner {

        // === Fields

        public StoryStep RunningStep { get; private set; }

        readonly Story _story;
        StepResult _waitingFor;
        
        StoryChapter _chapter;
        StoryChapter _chapterToChange;
        
        int _stepIndex;
        bool _executing;
        bool _stopped;

        // === Constructor

        public StepSequenceRunner(Story story) {
            _story = story;
        }

        // === Public interface

        public void ChangeChapter(StoryChapter chapter) {
            if (_executing) {
                _chapterToChange = chapter;
            } else {
                _chapter = chapter;
                _stepIndex = -1;
                _waitingFor = StepResult.Immediate;
            }
        }

        public void Advance() {
            while (!_stopped && _waitingFor.IsDone) {
                RunningStep = GetNextStep();
                if (RunningStep == null) {
                    return;
                }

                _executing = true;
                try {
                    _waitingFor = RunningStep.Execute(_story);
#if UNITY_EDITOR
                    RunningStep.DebugInfo?.SetResult(_waitingFor);
#endif
                } catch {
                    _stopped = true;
                    Log.Important?.Error($"Exception happened in graph: {_story?.Guid}");
                    throw;
                } finally {
                    _executing = false;
                }

                if (RunningStep.AutoPerformed) {
                    StoryUtilsRuntime.StepPerformed(_story, RunningStep);
                }

                if (_chapterToChange != null) {
                    ChangeChapter(_chapterToChange);
                    _chapterToChange = null;
                }
            }
        }

        public void Stop() {
            _stopped = true;
        }

        StoryStep GetNextStep() {
            do {
                ++_stepIndex;
                while (_stepIndex >= _chapter.steps.Length) {
                    if (_chapter.continuation == null) {
                        return null;
                    }
                    ChangeChapter(_chapter.continuation);
                    ++_stepIndex;
                }
            } while (StoryUtilsRuntime.ShouldExecute(_story, _chapter.steps[_stepIndex]) == false);
            return _chapter.steps[_stepIndex];
        }
    }
}