using System;
using Awaken.Kandra.AnimationPostProcess;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Object = UnityEngine.Object;

namespace Awaken.Kandra.Editor.AnimationPostProcess {
    public class AnimationPostProcessingEditorWindow : EditorWindow {
        AnimationPostProcessingPreset _preset;
        Transform _prefab;
        Transform _instance;

        BoneData[] _bones;
        Transform _boneToAdd;
        
        Vector2 _scroll;
        int _selectedBoneIndex = -1;

        [MenuItem("TG/Assets/Kandra/AnimPP Editor")]
        static void Open() {
            Open(null);
        }

        public static void Open(AnimationPostProcessingPreset preset) {
            var window = GetWindow<AnimationPostProcessingEditorWindow>();
            window._preset = preset;
            window.Show();
        }
        
        void OnGUI() {
            DrawSetup(out var isValid);
            if (isValid) {
                DrawPresets();
            }
        }

        void DrawSetup(out bool isValid) {
            bool setupChanged = false;
            setupChanged |= DrawObject("Preset", _preset, out _preset);
            setupChanged |= DrawObject("Prefab", _prefab, out _prefab);
            if (_preset == null || _prefab == null) {
                _bones = Array.Empty<BoneData>();
                _selectedBoneIndex = -1;
                isValid = false;
                return;
            }
            if (setupChanged | _instance == null) {
                RefreshPreview();
            }
            using (new EditorGUI.DisabledScope(true)) {
                DrawObject("Instance", _instance, out _);
            }

            isValid = true;
        }

        void DrawPresets() {
            const float BoneWidth = 120;
            const float VerticalPadding = 16;
            const float HorizontalSpacing = 12;
            GUILayout.Space(VerticalPadding);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Bone", GUILayout.Width(BoneWidth));
            GUILayout.Space(HorizontalSpacing);
            EditorGUILayout.LabelField("Position");
            GUILayout.Space(HorizontalSpacing);
            EditorGUILayout.LabelField("Scale");
            GUILayout.EndHorizontal();
            _scroll = GUILayout.BeginScrollView(_scroll);
            bool presetChanged = false;
            for (int i = 0; i < _preset.transformations.Length; i++) {
                ref var transformation = ref _preset.transformations[i];
                GUILayout.BeginHorizontal();
                using (new ColorGUIScope(_selectedBoneIndex == i ? Color.green : Color.white)) {
                    if (GUILayout.Button(transformation.bone.ToString(), GUILayout.Width(BoneWidth))) {
                        _selectedBoneIndex = _selectedBoneIndex != i ? i : -1;
                    }
                }
                GUILayout.Space(HorizontalSpacing);
                Undo.RecordObject(_preset, $"Modify Bone {_bones[i].bone.name}");
                var changed = false;
                changed |= DrawVector3(nameof(transformation.position), transformation.position, out transformation.position);
                GUILayout.Space(HorizontalSpacing);
                changed |= DrawVector3(nameof(transformation.scale), transformation.scale, out transformation.scale);
                if (changed) {
                    presetChanged = true;
                    ModifyBone(_bones[i], transformation);
                }
                if (GUILayout.Button("X", GUILayout.Width(35))) {
                    ArrayUtils.RemoveAt(ref _preset.transformations, i);
                    RefreshPreview();
                    presetChanged = true;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(VerticalPadding);
            if (DrawObject("Bone To Add", null, out Transform boneToAdd)) {
                if (AddBone(_preset, boneToAdd)) {
                    RefreshPreview();
                    presetChanged = true;
                }
            }
            GUILayout.EndScrollView();

            if (presetChanged) {
                EditorUtility.SetDirty(_preset);
            }
        }

        void OnSceneGUI(SceneView sceneView) {
            if (_selectedBoneIndex == -1) {
                return;
            }
            
            ref readonly var bone = ref _bones[_selectedBoneIndex];
            if (bone.bone == null) {
                return;
            }
            
            ref var transformation = ref _preset.transformations[_selectedBoneIndex];

            var localPosition = bone.originalLocalPosition + transformation.position;
            var localScale = Vector3Util.cmul(bone.originalLocalScale, transformation.scale);
            
            var previousMatrix = Handles.matrix;
            Handles.matrix = bone.bone.parent.localToWorldMatrix;
            var newLocalPosition = Handles.PositionHandle(localPosition, Quaternion.identity);
            Handles.matrix = bone.bone.localToWorldMatrix;
            var newLocalScale = Handles.ScaleHandle(localScale, Vector3.zero, Quaternion.identity, HandleUtility.GetHandleSize(Vector3.zero) * 0.6f);
            Handles.matrix = previousMatrix;

            Undo.RecordObject(_preset, $"Modify Bone {bone.bone.name}");
            bool changed = false;
            if (newLocalPosition != localPosition) {
                transformation.position = newLocalPosition - bone.originalLocalPosition;
                changed = true;
            }
            if (newLocalScale != localScale) {
                transformation.scale = Vector3Util.cdiv(newLocalScale, bone.originalLocalScale);
                changed = true;
            }
            if (changed) {
                ModifyBone(bone, transformation);
                EditorUtility.SetDirty(_preset);
                Repaint();
            }
        }
        
        void OnEnable() {
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += RefreshPreview;
        }

        void OnDisable() {
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= RefreshPreview;
            if (_instance) {
                DestroyImmediate(_instance.gameObject);
                _instance = null;
            }
        }

        void RefreshPreview() {
            if (_instance) {
                DestroyImmediate(_instance.gameObject);
            }
            _instance = Instantiate(_prefab);
            PrepareInstance(_instance);
            _bones = BuildBoneData(_instance, _preset);
            for (int i = 0; i < _preset.transformations.Length; i++) {
                ModifyBone(_bones[i], _preset.transformations[i]);
            }
        }

        public static void PrepareInstance(Transform instance) {
            var rigBuilder = instance.GetComponentInChildren<RigBuilder>();
            if (rigBuilder) {
                DestroyImmediate(rigBuilder);
            }
            var animator = instance.GetComponentInChildren<Animator>();
            if (animator) {
                DestroyImmediate(animator);
            }
        }

        static BoneData[] BuildBoneData(Transform root, AnimationPostProcessingPreset preset) {
            var bones = root.GetComponentsInChildren<Transform>();
            var boneData = new BoneData[preset.transformations.Length];
            for (int i = 0; i < preset.transformations.Length; i++) {
                var bone = Array.Find(bones, b => preset.transformations[i].bone.Equals(b.name));
                if (bone == null) {
                    Log.Important?.Error($"Bone {preset.transformations[i].bone} not found in {root}", root);
                    continue;
                }
                boneData[i] = new BoneData(bone);
            }
            return boneData;
        }
        
        static bool AddBone(AnimationPostProcessingPreset preset, Transform bone) {
            var name = bone.name;
            foreach (ref readonly var transformation in preset.transformations.RefIterator()) {
                if (transformation.bone == name) {
                    Log.Important?.Error($"Bone {name} already added");
                    return false;
                }
            }

            ArrayUtils.Add(ref preset.transformations, new AnimationPostProcessingPreset.Transformation {
                bone = name,
                position = Vector3.zero,
                scale = Vector3.one,
            });
            return true;
        }

        static void ModifyBone(in BoneData bone, in AnimationPostProcessingPreset.Transformation transformation) {
            if (bone.bone) {
                bone.bone.localPosition = bone.originalLocalPosition + transformation.position;
                bone.bone.localScale = Vector3Util.cmul(bone.originalLocalScale, transformation.scale);
            }
        }
        
        static bool DrawObject<T>(string label, T obj, out T newObj) where T : Object {
            newObj = EditorGUILayout.ObjectField(label, obj, typeof(T), true) as T;
            return newObj != obj;
        }

        static bool DrawVector3(string label, Vector3 vector, out Vector3 newVector) {
            newVector = EditorGUILayout.Vector3Field("", vector);
            return newVector != vector;
        }

        readonly struct BoneData {
            public readonly Transform bone;
            public readonly Vector3 originalLocalPosition;
            public readonly Vector3 originalLocalScale;
            
            public BoneData(Transform bone) {
                this.bone = bone;
                originalLocalPosition = bone.localPosition;
                originalLocalScale = bone.localScale;
            }
        }
    }
}