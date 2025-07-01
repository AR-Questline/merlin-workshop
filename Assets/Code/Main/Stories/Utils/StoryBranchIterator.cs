using System;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;

namespace Awaken.TG.Main.Stories.Utils {
    /// <summary>
    /// Iterates over story steps up to end or next choice.
    /// </summary>
    public struct StoryBranchIterator {
        StoryChapter _chapter;
        int _index;
        
        public StoryBranchIterator(StoryChapter chapter) {
            _chapter = chapter;
            _index = -1;
        }
        
        public StoryBranchIterator(StoryChapter chapter, StoryStep step) {
            _chapter = chapter;
            _index = Array.IndexOf(_chapter.steps, step);
        }

        public StoryBranchIterator GetEnumerator() => this;

        public bool MoveNext() {
            while (_chapter != null) {
                ++_index;
                if (_index < _chapter.steps.Length) {
                    if (_chapter.steps[_index] is SChoice) {
                        _chapter = null;
                        return false;
                    }
                    return true;
                }
                _chapter = _chapter.continuation;
                _index = -1;
            }
            return false;
        }

        public StoryStep Current => _chapter.steps[_index];
    }
}