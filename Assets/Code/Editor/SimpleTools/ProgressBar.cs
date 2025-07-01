using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Awaken.TG.Editor.SimpleTools {
    public abstract class ProgressBar : IDisposable {
        protected abstract string Info { get; set; }
        protected abstract string Title { get; }

        float _taken;
        bool _abortable;

        protected abstract float GlobalPercent(float percent);
        
        public void Display(float percent, string info = null) {
            if (_abortable) {
                if (EditorUtility.DisplayCancelableProgressBar(Title, info ?? Info, GlobalPercent(percent))) {
                    throw new Exception($"Process {Title} aborted");
                }
            } else {
                EditorUtility.DisplayProgressBar(Title, info ?? Info, GlobalPercent(percent));
            }
        }
        
        public void Display(int current, int total) {
            Display((float) current / total, $"{current.ToString()}/{total.ToString()}");
        }

        public bool DisplayCancellable(float percent, string info = null) {
            return EditorUtility.DisplayCancelableProgressBar(Title, info ?? Info, GlobalPercent(percent));
        }
        
        public bool DisplayCancellable(int current, int total) {
            return DisplayCancellable((float) current / total, $"{current.ToString()}/{total.ToString()}");
        }


        public ProgressBar TakeRest(string info = null) {
            return TakePart(1 - _taken, info);
        }
        public ProgressBar TakePart(float length, string info = null) {
            var bar = new PartialProgressBar(this, _taken, length, info, _abortable);
            _taken += length;
            return bar;
        }

        public ProgressBarIterator TakePartsFor(ICollection collection, float length, string info = null) {
            return new(TakePartsForEnumerator(length, collection.Count, info));
        }
        public ProgressBarIterator TakePartsFor(float length, int parts, string info = null) {
            return new(TakePartsForEnumerator(length, parts, info));
        }

        public ProgressBarIterator TakeRestsFor(ICollection collection, string info = null) {
            return TakePartsFor(collection, 1 - _taken, info);
        }
        public ProgressBarIterator TakeRestsFor(int parts, string info = null) {
            return TakePartsFor(1 - _taken, parts, info);
        }

        IEnumerator<ProgressBar> TakePartsForEnumerator(float length, int count, string info = null){
            float end = _taken + length;
            float percent = length / count;
            for (int i = 0; i < count - 1; i++) {
                yield return TakePart(percent, info);
            }
            yield return TakePart(end - _taken, info);
        }

        public ProgressBar Displayed(float percent) {
            Display(percent);
            return this;
        }
        
        public static ProgressBar Create(string title, string info = null, bool abortable = false) {
            return new FullProgressBar(title, info, abortable);
        }

        void IDisposable.Dispose() {
            OnDispose();
        }
        protected abstract void OnDispose();
        
        class FullProgressBar : ProgressBar {
            protected override string Info { get; set; }
            protected override string Title { get; }

            bool _cleared;

            public FullProgressBar(string title, string info, bool abortable) {
                Title = title;
                Info = info;
                _abortable = abortable;
            }
            ~FullProgressBar() {
                if (_cleared) {
                    EditorUtility.ClearProgressBar();
                }
            }

            protected override float GlobalPercent(float percent) {
                return percent;
            }

            protected override void OnDispose() {
                EditorUtility.ClearProgressBar();
                _cleared = true;
            }
        }

        class PartialProgressBar : ProgressBar {
            ProgressBar _bar;
            float _start;
            float _length;

            string _info;

            public PartialProgressBar(ProgressBar bar, float start, float length, string info, bool abortable) {
                _bar = bar;
                _start = start;
                _length = length;
                _info = info;
                _abortable = abortable;
            }

            protected override float GlobalPercent(float percent) {
                return _bar.GlobalPercent(_start + _length * percent);
            }

            protected override void OnDispose() {
                Display(1);
            }

            protected override string Info {
                get => _info ?? _bar.Info;
                set => _info = value;
            }

            protected override string Title => _bar.Title;
        }
        
        public class ProgressBarIterator : IDisposable {
            IEnumerator<ProgressBar> _bars;
            bool _disposed;

            public ProgressBarIterator(IEnumerator<ProgressBar> bars) {
                _bars = bars;
            }
            ~ProgressBarIterator() {
                Dispose();
            }

            public ProgressBar Next(string info = null) {
                if (_bars.MoveNext()) {
                    var bar = _bars.Current;
                    if (info != null) {
                        bar.Info = info;
                    }
                    return bar;
                } else {
                    throw new Exception("No more progress bar parts. Invalid implementation");
                }
            }

            public void Dispose() {
                if (_disposed) return;
                _disposed = true;
                _bars?.Dispose();
            }
        }
    }
}