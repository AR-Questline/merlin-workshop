using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Awaken.Utility.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.Utility.Profiling {
    public class WorldProfilerDetailsController : ProfilerModuleViewController {
        readonly SampleData[] _modelsData = {
            new() { name = ProfilerValues.ModelsCountName, lowerIsBetter = true },
            new() { name = ProfilerValues.AddedModelsName, lowerIsBetter = true },
            new() { name = ProfilerValues.RemovedModelsName, lowerIsBetter = false },
        };
        readonly SampleData[] _viewsData = {
            new() { name = ProfilerValues.SpawnedViewsCountName, lowerIsBetter = true },
            new() { name = ProfilerValues.BoundViewsCountName, lowerIsBetter = true },
            new() { name = ProfilerValues.SpawnedViewsName, lowerIsBetter = true },
            new() { name = ProfilerValues.DestroyedViewsName, lowerIsBetter = false },
            new() { name = ProfilerValues.BoundViewsName, lowerIsBetter = true },
            new() { name = ProfilerValues.UnboundViewsName, lowerIsBetter = false },
        };
        readonly SampleData[] _eventsData = {
            new() { name = ProfilerValues.EventsListenersCountName, lowerIsBetter = true },
            new() { name = ProfilerValues.AddedEventsListenersName, lowerIsBetter = true },
            new() { name = ProfilerValues.RemovedEventsListenersName, lowerIsBetter = false },
            new() { name = ProfilerValues.EventsCalledName, lowerIsBetter = true },
            new() { name = ProfilerValues.EventsSelectorsCalledName, lowerIsBetter = true },
            new() { name = ProfilerValues.EventsCallbacksCalledName, lowerIsBetter = true },
            new() { name = ProfilerValues.EventsCallsMaxDepthName, lowerIsBetter = true },
        };
        readonly SampleData[] _timingData = {
            new() { name = "PlayerLoop", lowerIsBetter = true },
            new() { name = "Gfx.WaitForPresentOnGfxThread", lowerIsBetter = true },
            new() { name = "BehaviourUpdate", lowerIsBetter = true },
            new() { name = "LateBehaviourUpdate", lowerIsBetter = true },
            new() { name = "FixedBehaviourUpdate", lowerIsBetter = true },
            new() { name = "FixedUpdate.PhysicsFixedUpdate", lowerIsBetter = true },
            new() { name = "UniTaskLoopRunnerYieldUpdate", lowerIsBetter = true },
        };
        readonly List<RawFrameDataView> _statisticFrames = new();

        List<MultiColumnListView> _views = new();
        SliderInt _framesSlider;
        Toggle _doubleSidedToggle;

        public WorldProfilerDetailsController(ProfilerWindow profilerWindow) : base(profilerWindow) {}
        
        protected override VisualElement CreateView() {
            var root = new VisualElement {
                style = {
                    marginTop = 8, marginLeft = 8, marginRight = 8, flexDirection = new(FlexDirection.Column),
                },
            };
            
            var top = new VisualElement {
                style = {
                    marginTop = 8, flexDirection = new(FlexDirection.Row),
                },
            };
            
            var bottom = new VisualElement {
                style = {
                    marginTop = 8, flexDirection = new(FlexDirection.Row),
                },
            };
            
            root.Add(top);
            root.Add(bottom);

            _framesSlider = new("Stats frames range", 4, 80) {
                value = 20, style = { flexGrow = 1 },
            };
            _framesSlider.RegisterValueChangedCallback(StatsRangeChanged);
            top.Add(_framesSlider);

            _doubleSidedToggle = new("Double sided stats");
            _doubleSidedToggle.RegisterValueChangedCallback(DoubleSidedChanged);
            top.Add(_doubleSidedToggle);

            _views.Add(CreateList(bottom, _modelsData, "Models"));
            _views.Add(CreateList(bottom, _viewsData, "Views"));
            _views.Add(CreateList(bottom, _eventsData, "Events"));
            _views.Add(CreateList(bottom, _timingData, "Times"));

            Refresh();

            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            return root;
        }

        protected override void Dispose(bool disposing) {
            if (!disposing) {
                return;
            }

            _framesSlider.UnregisterValueChangedCallback(StatsRangeChanged);

            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;

            base.Dispose(true);
        }
        
        void DoubleSidedChanged(ChangeEvent<bool> _) {
            Refresh();
        }

        void StatsRangeChanged(ChangeEvent<int> _) {
            Refresh();
        }
        
        void OnSelectedFrameIndexChanged(long _) {
            Refresh();
        }
        
        void Refresh() {
            if (ProfilerWindow.firstAvailableFrameIndex == -1) {
                return;
            }
            
            var frame = System.Convert.ToInt32(ProfilerWindow.selectedFrameIndex);
            var frameView = CollectStatsFrames(frame);

            UpdateProfileData(frameView, _statisticFrames, _modelsData);
            UpdateProfileData(frameView, _statisticFrames, _viewsData);
            UpdateProfileData(frameView, _statisticFrames, _eventsData);

            UpdateTimings(frameView, _statisticFrames);

            foreach (MultiColumnListView view in _views) {
                view.RefreshItems();
            }
        }
        
        static MultiColumnListView CreateList(VisualElement root, SampleData[] source, string headerTitle) {
            var columns = new Columns();
            Column nameColumn = CreateColumn("Name");
            nameColumn.bindCell += (cell, index) => ((Label)cell).text = source[index].name;

            var countColumn = CreateColumn("Count");
            countColumn.bindCell += (cell, index) => ((Label)cell).text = source[index].value.ToString("0.##", CultureInfo.InvariantCulture);

            var avgColumn = CreateColumn("Avg");
            avgColumn.bindCell += (cell, index) =>
                ((Label)cell).text = source[index].avg.ToString("0.##", CultureInfo.InvariantCulture);

            var deltaColumn = CreateColumn("Delta");
            deltaColumn.bindCell += (cell, index) => {
                var label = (Label)cell;
                var delta = source[index].Delta;
                label.text = delta.ToString("+0.##;-0.##;0.##", CultureInfo.InvariantCulture);
                label.style.color = source[index].DeltaColor();
            };

            columns.Add(nameColumn);
            columns.Add(countColumn);
            columns.Add(avgColumn);
            columns.Add(deltaColumn);
            var listView = new MultiColumnListView(columns) {
                style = { flexGrow = 1 },
            };
            root.Add(listView);
            listView.columns.resizePreview = true;
            listView.itemsSource = source;
            listView.showBorder = true;
            listView.showFoldoutHeader = true;
            listView.showBoundCollectionSize = false;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            listView.headerTitle = headerTitle;

            return listView;
        }
        
        static Column CreateColumn(string title) {
            var nameColumn = new Column {
                title = title,
                stretchable = true,
            };
            return nameColumn;
        }
        
        RawFrameDataView CollectStatsFrames(int frame) {
            var frameView = ProfilerDriver.GetRawFrameDataView(frame, 0);
            _statisticFrames.Clear();
            var rangeMin = _framesSlider.value;
            var rangeMax = 0;
            if (_doubleSidedToggle.value) {
                rangeMin /= 2;
                rangeMax = rangeMin;
            }
            var minFrame = Mathf.Clamp(frame - rangeMin, ProfilerDriver.firstFrameIndex, ProfilerDriver.lastFrameIndex);
            var maxFrame = Mathf.Clamp(frame + rangeMax, ProfilerDriver.firstFrameIndex, ProfilerDriver.lastFrameIndex);
            for (var i = minFrame; i < maxFrame; i++) {
                if (i == frame) {
                    continue;
                }
                var statsFrameView = ProfilerDriver.GetRawFrameDataView(i, 0);
                if (statsFrameView != null) {
                    _statisticFrames.Add(statsFrameView);
                }
            }
            return frameView;
        }

        static void UpdateProfileData(FrameDataView frameView, List<RawFrameDataView> statsFrames, SampleData[] data) {
            for (int i = 0; i < data.Length; i++) {
                ref var datum = ref data[i];
                var markerId = frameView.GetMarkerId(datum.name);
                datum.value = frameView.GetCounterValueAsInt(markerId);
                datum.avg = (float)statsFrames.Average(f => f.GetCounterValueAsInt(markerId));
            }
        }
        
        void UpdateTimings(RawFrameDataView frameView, List<RawFrameDataView> statsFrames) {
            for (var i = 0; i < _timingData.Length; i++) {
                ref var timingDatum = ref _timingData[i];
                var id = frameView.GetMarkerId(timingDatum.name);
                float time = CollectMarkerTime(frameView, id);
                timingDatum.value = time;
                timingDatum.avg = statsFrames.Average(f => CollectMarkerTime(f, id));
            }
        }
        
        static float CollectMarkerTime(RawFrameDataView frameView, int id) {
            var time = 0f;
            for (var i = 0; i < frameView.sampleCount; i++) {
                if (frameView.GetSampleMarkerId(i) == id) {
                    time += frameView.GetSampleTimeMs(i);
                }
            }
            return time;
        }

        struct SampleData {
            internal string name;
            internal bool lowerIsBetter;
            internal float value;
            internal float avg;
            internal float Delta => value - avg;

            internal Color DeltaColor() {
                var delta = Delta;
                if (delta == 0) {
                    return Color.white;
                }
                return delta < 0 == lowerIsBetter ? Color.blue : Color.red;
            }
        }
    }
}
