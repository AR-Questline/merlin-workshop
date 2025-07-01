using System;
using System.Collections.Generic;
using Awaken.TG.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace Awaken.Utility.Debugging {
    public class FrameStatsWindow : UGUIWindowDisplay<FrameStatsWindow> {
        List<NamedProfilerRecorder> _recorders = new List<NamedProfilerRecorder>();

        void Awake() {
            var availableStatHandles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(availableStatHandles);
            foreach (var h in availableStatHandles) {
                var description = ProfilerRecorderHandle.GetDescription(h);
                if (description.Category != ProfilerCategory.Render) {
                    continue;
                }
                _recorders.Add(new(description));
            }
        }

        void OnDestroy() {
            for (int i = 0; i < _recorders.Count; i++) {
                _recorders[i].Dispose();
            }
            _recorders.Clear();
        }

        protected override void DrawWindow() {
            foreach (var recorder in _recorders) {
                if (recorder.recorder.Valid && SearchContext.HasSearchInterest(recorder.description.Name)) {
                    recorder.Draw();
                }
            }
        }

        struct NamedProfilerRecorder : IDisposable {
            public readonly ProfilerRecorderDescription description;
            public ProfilerRecorder recorder;

            public string Name => description.Name;
            public ProfilerMarkerDataUnit UnitType => description.UnitType;

            public NamedProfilerRecorder(ProfilerRecorderDescription description) {
                this.description = description;
                this.recorder = ProfilerRecorder.StartNew(description.Category, description.Name);
            }

            public void Draw() {
                if (UnitType == ProfilerMarkerDataUnit.Bytes) {
                    GUILayout.Label($"{Name} - {M.HumanReadableBytes((ulong)recorder.LastValue)}");
                } else if (UnitType == ProfilerMarkerDataUnit.Count) {
                    GUILayout.Label($"{Name} - {recorder.LastValue:N0}");
                } else if (UnitType == ProfilerMarkerDataUnit.Percent) {
                    GUILayout.Label($"{Name} - {recorder.LastValue:P}");
                } else if (UnitType == ProfilerMarkerDataUnit.TimeNanoseconds) {
                    var timeValue = (ulong)recorder.LastValue;
                    if (timeValue > 10_000) {
                        var msValue = timeValue / 1_000_000f;
                        GUILayout.Label($"{Name} - {msValue:f2} ms");
                    } else {
                        var microValue = timeValue / 1_000f;
                        GUILayout.Label($"{Name} - {microValue:f2} μs");
                    }
                } else {
                    GUILayout.Label($"{Name} - {recorder.LastValue}");
                }
            }

            public void Dispose() {
                recorder.Dispose();
            }
        }
    }
}
