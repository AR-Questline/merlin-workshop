using System;
using System.Collections.Generic;
using System.Text;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.GameObjects;
using Awaken.Utility.UI;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Pool;

namespace Awaken.TG.Debugging {
    public class MemoryInfo : UGUIWindowDisplay<MemoryInfo> {
        bool _showDebug;
        OnDemandCache<IMainMemorySnapshotProvider, Vector2> _memoryProviderScrolls = new(static _ => Vector2.zero);

        protected override bool WithSearch => false;
        protected override bool WithScroll => false;

        [StaticMarvinButton(state: nameof(IsMemoryInfoOn))]
        static void ToggleMemoryInfo() {
            MemoryInfo.Toggle(new UGUIWindowUtils.WindowPositioning(UGUIWindowUtils.WindowPosition.TopLeft, 0.75f, 0.6f));
        }

        static bool IsMemoryInfoOn() => MemoryInfo.IsShown;

        protected override void Initialize() {
            base.Initialize();
            UnityDataBootstrap();
        }

        protected override void Shutdown() {
            AStarMemoryInfo.Clear();
            UnityDataTeardown();
            _memoryProviderScrolls.Clear();
            base.Shutdown();
        }

        protected override void DrawWindow() {
            const float MarginPaddingMultiplier = 0.94f;

            var unityInfoWidth = math.min(270, Position.width * 0.2f);
            var memorySnapshotsWidth = (Position.width - unityInfoWidth) * 0.6f;
            var othersWidth = Position.width - memorySnapshotsWidth - unityInfoWidth;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Width(memorySnapshotsWidth * MarginPaddingMultiplier));
            DrawMemorySnapshotsInfo();
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.Width(othersWidth * MarginPaddingMultiplier));
            DrawAddressablesInfo();
            GUILayout.Space(4);
            DrawAStarInfo();
            GUILayout.Space(4);
            DrawDistanceCullerInfo();
            GUILayout.EndVertical();
            
            GUILayout.BeginVertical("box", GUILayout.Width(unityInfoWidth * MarginPaddingMultiplier));
            DrawUnityInfo();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        // === Memory snapshots
        void DrawMemorySnapshotsInfo() {
            foreach (var mainProvider in IMainMemorySnapshotProvider.Providers) {
                var scroll = _memoryProviderScrolls[mainProvider];
                _memoryProviderScrolls[mainProvider] = GUILayout.BeginScrollView(scroll);
                MemorySnapshotMemoryInfo.DrawOnGUI(mainProvider);
                GUILayout.EndScrollView();
            }
        }

        // == Addressables
        static Vector2 s_addressablesScroll;
        static AddressablesBucketData s_allAssets;
        static AddressablesBucketData s_assets;
        static AddressablesBucketData s_nullAssets;
        static AddressablesBucketData s_nullNames;
        static AddressablesBucketData s_nullGOs;
        static AddressablesBucketData s_nullOthers;

        void DrawAddressablesInfo() {
            s_addressablesScroll = GUILayout.BeginScrollView(s_addressablesScroll);
            GUILayout.Label("Addressables:");

            GUILayout.BeginVertical("box");
            if (AddressablesInfo.Instance.TrackingData == null) {
                GUILayout.Label("No tracking data");
            } else {
                DrawPageableAddressablesData(ref s_allAssets, AddressablesInfo.Instance.TrackingData, "All loaded");
                DrawPageableAddressablesData(ref s_assets, AddressablesInfo.Instance.TrackedAssets, "Assets loaded");
                DrawPageableAddressablesData(ref s_nullAssets, AddressablesInfo.Instance.NullAssets, "Null assets");
                DrawPageableAddressablesData(ref s_nullNames, AddressablesInfo.Instance.NullAssetsNames, "Null names");
                DrawPageableAddressablesData(ref s_nullGOs, AddressablesInfo.Instance.NullGameObjectAssets, "Null GOs");
                DrawPageableAddressablesData(ref s_nullOthers, AddressablesInfo.Instance.OtherNullsAssets, "Null resources");
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            if (GUILayout.Button("Load tracking")) {
                AddressablesInfo.Instance.LoadTrackingData();
            }
            if (AddressablesInfo.Instance.TrackingData != null) {
                if (GUILayout.Button("Bake tracked")) {
                    AddressablesInfo.Instance.BakeTracked();
                }
                if (GUILayout.Button("Clear")) {
                    AddressablesInfo.Instance.Clear();
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        static void DrawPageableAddressablesData<T>(ref AddressablesBucketData data, IList<T> assets, string label) {
            data.expanded = TGGUILayout.Foldout(data.expanded, $"{label}: {assets.Count}");
            if (data.expanded) {
                data.page = TGGUILayout.PagedList(assets, static (i, a) => GUILayout.Label($"{i}. {a}"), data.page);
            }
        }

        static void DrawPageableAddressablesData<T>(ref AddressablesBucketData data, ICollection<T> assets, string label) {
            data.expanded = TGGUILayout.Foldout(data.expanded, $"{label}: {assets.Count}");
            if (data.expanded) {
                data.page = TGGUILayout.PagedList(assets, static (i, a) => GUILayout.Label($"{i}. {a}"), data.page);
            }
        }

        struct AddressablesBucketData {
            internal bool expanded;
            internal int page;
        }

        // === AStar
        static Vector2 s_aStarScroll;
        void DrawAStarInfo() {
            s_aStarScroll = GUILayout.BeginScrollView(s_aStarScroll, GUILayout.MaxHeight(120), GUILayout.ExpandHeight(false));
            GUILayout.Label("AStar:");

            var aStar = AstarPath.active;
            if (aStar != null && aStar.graphs != null) {
                foreach (var graph in aStar.graphs) {
                    AStarMemoryInfo.DrawOnGUI(graph);
                }
            }

            GUILayout.EndScrollView();
        }
        
        // === DistanceCuller
        static Vector2 s_distanceCullerScroll;
        void DrawDistanceCullerInfo() {
            s_distanceCullerScroll = GUILayout.BeginScrollView(s_distanceCullerScroll, GUILayout.MaxHeight(120));
            GUILayout.Label("DistanceCullers:");
            ListPool<DistanceCuller>.Get(out var alreadyDrawnCullers);
            alreadyDrawnCullers.Clear();
            foreach (var culler in World.Services.Get<DistanceCullersService>().Cullers) {
                if (alreadyDrawnCullers.Contains(culler)) {
                    continue;
                }
                DistanceCuller.DistanceCullerMemoryInfo.DrawOnGUI(culler);
                alreadyDrawnCullers.Add(culler);
            }
            alreadyDrawnCullers.Clear();
            ListPool<DistanceCuller>.Release(alreadyDrawnCullers);
            GUILayout.EndScrollView();
        }
        
        // === Unity
        bool _unityExpanded;
        StringBuilder _unityInfoBuilder = new();
        List<NamedProfilerRecorder> _recorders = new();

        void UnityDataBootstrap() {
            var availableStatHandles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(availableStatHandles);
            foreach (var h in availableStatHandles) {
                var description = ProfilerRecorderHandle.GetDescription(h);
                if (description.Category != ProfilerCategory.Memory) {
                    continue;
                }
                _recorders.Add(new(description.Name, description.UnitType == ProfilerMarkerDataUnit.Count));
            }
        }
        
        void UnityDataTeardown() {
            for (int i = 0; i < _recorders.Count; i++) {
                _recorders[i].Dispose();
            }
            _recorders.Clear();
        }
        
        void DrawUnityInfo() {
            _unityExpanded = TGGUILayout.Foldout(_unityExpanded, "Unity");
            if (!_unityExpanded) {
                return;
            }
            for (var i = 0; i < _recorders.Count; i++) {
                _unityInfoBuilder.Append(_recorders[i].name);
                _unityInfoBuilder.Append(": ");
                if (_recorders[i].recorder.Valid) {
                    if (!_recorders[i].isCount) {
                        _unityInfoBuilder.Append(M.HumanReadableBytes((ulong)_recorders[i].recorder.LastValue));
                    } else {
                        _unityInfoBuilder.Append(_recorders[i].recorder.LastValue);
                    }
                } else {
                    _unityInfoBuilder.Append("is broken");
                }
                if (i < _recorders.Count - 1) {
                    _unityInfoBuilder.AppendLine();
                }
            }
            
            GUILayout.Label(_unityInfoBuilder.ToString());
            GUILayout.Label($"Addessables memory budget: {M.HumanReadableBytes(AssetBundle.memoryBudgetKB*1024)}");

            _unityInfoBuilder.Clear();
        }

        struct NamedProfilerRecorder : IDisposable {
            public readonly string name;
            public readonly bool isCount;
            public ProfilerRecorder recorder;
            
            public NamedProfilerRecorder(string name, bool isCount) {
                this.name = name;
                this.isCount = isCount;
                this.recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, name);
            }
            
            public void Dispose() {
                recorder.Dispose();
            }
        }
    }
}
