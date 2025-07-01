using System;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    public class FbxAnimationClipsImportEditor : OdinEditorWindow {
        static readonly (string, string)[] SettingsConfig = {
            (nameof(ModelImporterClipAnimation.loopTime), ""),
            (nameof(ModelImporterClipAnimation.loopPose), ""),
            (nameof(ModelImporterClipAnimation.cycleOffset), ""),

            (nameof(ModelImporterClipAnimation.lockRootRotation), "Bake Root Rotation"),
            (nameof(ModelImporterClipAnimation.keepOriginalOrientation), "Based Upon Root Rotation"),
            (nameof(ModelImporterClipAnimation.rotationOffset), "Offset Root Rotation"),

            (nameof(ModelImporterClipAnimation.lockRootHeightY), "Bake Root Position (Y)"),
            (nameof(ModelImporterClipAnimation.keepOriginalPositionY), "Based Upon Root Position (Y)"),
            (nameof(ModelImporterClipAnimation.heightOffset), "Offset Root Position (Y)"),

            (nameof(ModelImporterClipAnimation.lockRootPositionXZ), "Bake Root Position (XZ)"),
            (nameof(ModelImporterClipAnimation.keepOriginalPositionXZ), "Based Upon Root Position (XZ)"),
        };

        ModelImporter _currentModelImporter;
        string _oldSelectedName;

        [ShowInInspector, PropertyOrder(0)] Object _fbxSource;

        [HorizontalGroup("Horizontal", 0.65f), PropertyOrder(2)]
        [ShowInInspector, OnValueChanged(nameof(ClipChanged), true)]
        [TableList(AlwaysExpanded = true, IsReadOnly = true)]
        ClipData[] _allClips = Array.Empty<ClipData>();

        [HorizontalGroup("Horizontal", 0.35f), PropertyOrder(3)]
        [ShowInInspector, TableList(AlwaysExpanded = true, IsReadOnly = true)]
        SettingsToCopy[] _settings;

        protected override void OnEnable() {
            base.OnEnable();
            CreateSettings();
            SelectionChanged();
            Selection.selectionChanged -= SelectionChanged;
            Selection.selectionChanged += SelectionChanged;
        }

        [Button, EnableIf(nameof(CanApply)), PropertyOrder(1)]
        void Apply() {
            var source = _allClips.First(static c => c.isSource).clipImporter;
            source = _currentModelImporter.clipAnimations.First(c => c.name == source.name);
            foreach (var clipData in _allClips) {
                if (!clipData.isDestination && !clipData.isSource) {
                    continue;
                }
                foreach (var setting in _settings) {
                    setting.Copy(source, clipData.clipImporter);
                }
            }
            _currentModelImporter.clipAnimations = _allClips.Select(static c => c.clipImporter).ToArray();
            EditorUtility.SetDirty(_currentModelImporter);
            _currentModelImporter.SaveAndReimport();
        }

        void SelectionChanged() {
            var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(selectedPath)) {
                ClearOldData();
                return;
            }
            var modelImporter = AssetImporter.GetAtPath(selectedPath) as ModelImporter;
            if (modelImporter == null) {
                ClearOldData();
                return;
            }
            if (_currentModelImporter == modelImporter) {
                return;
            }
            ClearOldData();

            _fbxSource = Selection.activeObject;
            _currentModelImporter = modelImporter;
            bool first = true;
            _allClips = modelImporter.clipAnimations.Select(clip => {
                    var clipData = new ClipData(clip, !first);
                    first = false;
                    return clipData;
                })
                .ToArray();
        }

        void ClearOldData() {
            _fbxSource = null;
            _currentModelImporter = null;
            _allClips = Array.Empty<ClipData>();
        }

        void ClipChanged() {
            foreach (var clipData in _allClips) {
                if (clipData.isSource && clipData.isDestination) {
                    clipData.isSource = false;
                }
            }
            if (_allClips.Count(static c => c.isSource) > 1) {
                _allClips.First(c => c.name == _oldSelectedName).isSource = false;
            }
            _oldSelectedName = _allClips.FirstOrDefault(static c => c.isSource)?.name;
        }

        void CreateSettings() {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty;
            _settings = new SettingsToCopy[SettingsConfig.Length];
            for (var i = 0; i < SettingsConfig.Length; i++) {
                var property = typeof(ModelImporterClipAnimation).GetProperty(SettingsConfig[i].Item1, Flags);
                _settings[i] = new SettingsToCopy(property, SettingsConfig[i].Item2);
            }
        }

        bool CanApply() {
            return _allClips.Any(static c => c.isSource) && _allClips.Any(static c => c.isDestination);
        }

        class ClipData {
            [HideInTables] public ModelImporterClipAnimation clipImporter;
            [ReadOnly] public string name;
            [TableColumnWidth(55, Resizable = false)] public bool isSource;
            [TableColumnWidth(75, Resizable = false)] public bool isDestination;

            public ClipData(ModelImporterClipAnimation clip, bool asDestination) {
                clipImporter = clip;
                name = clip.name;
                isSource = !asDestination;
                isDestination = asDestination;
            }
        }

        class SettingsToCopy {
            [ReadOnly] public string name;
            [OnValueChanged(nameof(CopyChanged)), TableColumnWidth(40, Resizable = false)]
            public bool copy;

            [HideInTables] PropertyInfo _propertyInfo;

            string Key => $"{nameof(FbxAnimationClipsImportEditor)}_{_propertyInfo.Name}";

            public SettingsToCopy(PropertyInfo propertyInfo, string nameOverride) {
                _propertyInfo = propertyInfo;
                name = string.IsNullOrWhiteSpace(nameOverride) ? ObjectNames.NicifyVariableName(propertyInfo.Name) : nameOverride;
                copy = EditorPrefs.GetBool(Key, false);
            }

            public void Copy(ModelImporterClipAnimation source, ModelImporterClipAnimation destination) {
                if (!copy) {
                    return;
                }
                var sourceValue = _propertyInfo.GetValue(source);
                _propertyInfo.SetValue(destination, sourceValue);
            }

            void CopyChanged() {
                EditorPrefs.SetBool(Key, copy);
            }
        }

        // === Editor show
        [InitializeOnLoadMethod]
        static void InitHeader() {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnPostHeaderGUI;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        static void OnPostHeaderGUI(UnityEditor.Editor editor) {
            if (editor.targets.Length != 1) {
                return;
            }
            if (editor.target is not ModelImporter modelImporter) {
                return;
            }
            if (!modelImporter.importAnimation) {
                return;
            }
            if (modelImporter.clipAnimations.Length < 2) {
                return;
            }
            if (GUILayout.Button("Show FBX Animation Clips Importer")) {
                EditorWindow.GetWindow<FbxAnimationClipsImportEditor>().Show();
            }
        }
    }
}
