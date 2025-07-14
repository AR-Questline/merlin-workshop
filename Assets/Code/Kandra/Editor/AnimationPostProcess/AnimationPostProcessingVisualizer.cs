using System;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.Helpers;
using Awaken.Utility.Maths;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor.AnimationPostProcess {
    public class AnimationPostProcessingVisualizer : AREditorWindow {
        [SerializeField] bool autoRefresh = true;
        [SerializeField] GameObject prefab;
        [SerializeField] Kandra.AnimationPostProcess.AnimationPostProcessing.Entry[] entries = Array.Empty<Kandra.AnimationPostProcess.AnimationPostProcessing.Entry>();

        Transform _instance;

        protected override void OnEnable() {
            base.OnEnable();

            AddButton("Refresh", Refresh);
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (_instance) {
                DestroyImmediate(_instance.gameObject);
            }
        }

        protected override void OnGUI() {
            EditorGUI.BeginChangeCheck();
            base.OnGUI();
            if (EditorGUI.EndChangeCheck()) {
                if (autoRefresh) {
                    Refresh();
                }
            }
        }

        void Refresh() {
            if (_instance) {
                DestroyImmediate(_instance.gameObject);
            }
            
            if (prefab == null) {
                return;
            }
            _instance = Instantiate(prefab.transform);
            AnimationPostProcessingEditorWindow.PrepareInstance(_instance);
            
            var bones = _instance.GetComponentsInChildren<Transform>();
            var boneNames = ArrayUtils.Select(bones, bone => {
                var boneName = new FixedString32Bytes();
                boneName.CopyFromTruncated(bone.name);
                return boneName;
            });
            
            foreach (ref readonly var entry in entries.RefIterator()) {
                if (entry.preset == null) {
                    continue;
                }
                foreach (ref readonly var transformation in entry.preset.transformations.RefIterator()) {
                    var index = Array.IndexOf(boneNames, transformation.bone);
                    if (index == -1) {
                        Log.Important?.Error($"Bone {transformation.bone} not found in Prefab {prefab}", _instance, LogOption.NoStacktrace);
                        continue;
                    }
                    var bone = bones[index];
                    bone.localPosition += transformation.position * entry.weight;
                    bone.localScale = Vector3Util.cmul(bone.localScale, math.pow(transformation.scale, entry.weight));
                }
            }
        }
        
        [MenuItem("TG/Assets/Kandra/AnimPP Visualizer")]
        static void Open() {
            GetWindow<AnimationPostProcessingVisualizer>().Show();
        }
    }
}