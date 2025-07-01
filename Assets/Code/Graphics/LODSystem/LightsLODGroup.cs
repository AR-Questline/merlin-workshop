using System;
using System.Linq;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Settings.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.LODSystem {
    [DefaultExecutionOrder(2000)]
    public class LightsLODGroup : StartDependentView<CameraStateStack> {
        [SerializeField, ListDrawerSettings(ShowFoldout = false, ShowPaging = false, CustomAddFunction = nameof(AddEntry)), OnValueChanged(nameof(EntriesChanged))]
        LodEntry[] _entries = Array.Empty<LodEntry>();
        FloatRange[] _sqrRanges;
        int _lastEntry = -1;
        bool _preparedEntries;
        
        Transform _cameraTransform;
        Transform _transform;

        // === Initialize
        void Start() {
            PrepareEntries();
        }

        protected override void OnInitialize() {
            CreateRanges();

            _cameraTransform = Target.MainCamera.transform;
            _transform = transform;
            
            PrepareEntries();
        }
        
        void CreateRanges() {
            _sqrRanges = new FloatRange[_entries.Length];
            var startDistance = 0f;
            for (var i = 0; i < _entries.Length; i++) {
                var entry = _entries[i];
                var sqrEndDistance = entry.endDistance*entry.endDistance;
                _sqrRanges[i] = new(startDistance, sqrEndDistance);
                startDistance = sqrEndDistance;
            }
        }

        void PrepareEntries() {
            if (_preparedEntries) {
                return;
            }
            _preparedEntries = true;
            for (int i = 0; i < _entries.Length; i++) {
                SetEntryState(i, false);
            }
        }

        // === Logic
        void Update() {
            var sqrDistance = (_transform.position - _cameraTransform.position).sqrMagnitude;
            var currentEntry = -1;
            for (var i = 0; i < _sqrRanges.Length; i++) {
                if (_sqrRanges[i].Contains(sqrDistance)) {
                    currentEntry = i;
                }
            }
            if (currentEntry == _lastEntry) {
                return;
            }
            if (_lastEntry > -1) {
                SetEntryState(_lastEntry, false);
            }
            _lastEntry = currentEntry;
            if (_lastEntry > -1) {
                SetEntryState(currentEntry, true);
            }
        }

        void SetEntryState(int index, bool state) {
            foreach (var entryLight in _entries[index].lights) {
                entryLight.SetActive(state);
            }
        }
        
        // === Odin/Editor methods
        void Reset() {
            _entries = new LodEntry[transform.childCount];
            for (int i = 0; i < _entries.Length; i++) {
                _entries[i] = new() {
                    endDistance = (i+1)*200,
                    name = $"LOD{i}",
                    lights = new[] { transform.GetChild(i).gameObject },
                };
            }
        }

        void EntriesChanged() {
            for (int i = 0; i < _entries.Length; i++) {
                _entries[i].name = $"LOD{i}";
            }
        }

        LodEntry AddEntry() {
            return new() {
                name = $"LOD{_entries.Length}",
                endDistance = (_entries.LastOrDefault()?.endDistance ?? 0) + 200,
            };
        }

#if UNITY_EDITOR
        static readonly Color[] Colors = {
            new(0.051f, 0.051f, 0.353f), new(0.231f, 0.522f, 0.329f), new(0f, 0.204f, 0.302f),
            new(0.165f, 0.588f, 0.337f), new(0.408f, 0.176f, 0.741f), new(0.637f, 0.396f, 0.051f),
            new(0.776f, 0.753f, 0.996f), new(0.373f, 0, 0.306f), new(0.082f, 0.592f, 0.302f),
        };
        static Lazy<GUIStyle> s_boxStyle = new(() => {
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = Texture2D.whiteTexture;
            style.normal.scaledBackgrounds = Array.Empty<Texture2D>();
            style.active.background = Texture2D.whiteTexture;
            style.active.scaledBackgrounds = Array.Empty<Texture2D>();
            style.focused.background = Texture2D.whiteTexture;
            style.focused.scaledBackgrounds = Array.Empty<Texture2D>();
            style.hover.background = Texture2D.whiteTexture;
            style.hover.scaledBackgrounds = Array.Empty<Texture2D>();
            
            style.alignment = TextAnchor.MiddleCenter;
            return style;
        });
        
        [PropertyOrder(-1), CustomValueDrawer("LodVisualizationDrawer"), ShowInInspector]
        int _dummy;
        
        int LodVisualizationDrawer(int _, GUIContent __) {
            GUILayout.Space(2);
            var max = _entries.Max(e => e.endDistance);
            var rect = GUILayoutUtility.GetAspectRect(14f);
            var pixelsPerUnit = rect.width/max;
            rect.width = 0;
            var previousStart = 0f;
            var color = GUI.backgroundColor;
            for (var i = 0; i < _entries.Length; i++) {
                rect.x += rect.width;
                rect.width = (_entries[i].endDistance - previousStart)*pixelsPerUnit;
                previousStart = _entries[i].endDistance;
                GUI.backgroundColor = Colors[i];
                GUI.Box(rect, _entries[i].name, s_boxStyle.Value);
            }
            GUI.backgroundColor = color;
            
            GUILayout.Space(6);

            var cameraTransform = _cameraTransform ? _cameraTransform : Camera.main?.transform;
            if (cameraTransform) {
                GUILayout.Label($"Current distance: {Vector3.Distance(transform.position, cameraTransform.position):f2}");
                GUILayout.Space(6);
            }
            
            return 0;
        }
#endif

        // === Helper classes
        [Serializable]
        class LodEntry {
            [HideInInspector] public string name;
            [BoxGroup("$name")] public float endDistance;
            [BoxGroup("$name"), ListDrawerSettings(AlwaysAddDefaultValue = true, ShowFoldout = false, ShowPaging = false)]
            public GameObject[] lights;
        }
    }
}
