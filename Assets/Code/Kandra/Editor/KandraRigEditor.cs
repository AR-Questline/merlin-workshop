using System;
using System.Collections.Generic;
using Awaken.Utility.UI;
using Unity.Collections;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    [CustomEditor(typeof(KandraRig))]
    public class KandraRigEditor : UnityEditor.Editor {
        SerializedProperty _animatorProperty;

        bool _bonesFoldout;

        bool _debugFoldout;
        ReorderableList _mergedRenderersList;
        ReorderableList _activeRenderersList;

        void OnEnable() {
            _animatorProperty = serializedObject.FindProperty("animator");

            var rig = (KandraRig)target;
            ValidateBones(rig);

            var mergedRenderers = KandraRig.EditorAccessor.MergedRenderers(rig);
            _mergedRenderersList = new ReorderableList(mergedRenderers, typeof(KandraRenderer), false, true, false, false);
            _mergedRenderersList.drawHeaderCallback = rect => {
                EditorGUI.LabelField(rect, "Merged Renderers");
            };

            var activeRenderers = KandraRig.EditorAccessor.ActiveRenderers(rig);
            _activeRenderersList = new ReorderableList(activeRenderers, typeof(KandraRenderer), false, true, false, false);
            _activeRenderersList.drawHeaderCallback = rect => {
                EditorGUI.LabelField(rect, "Active Renderers");
            };

            _mergedRenderersList.drawElementCallback += DrawKandraRendererElement(mergedRenderers);
            _activeRenderersList.drawElementCallback += DrawKandraRendererElement(activeRenderers);
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.PropertyField(_animatorProperty);
            serializedObject.ApplyModifiedProperties();

            DrawBones();

            DrawDebug();
        }

        void DrawBones() {
            var rig = (KandraRig)target;
            _bonesFoldout = EditorGUILayout.Foldout(_bonesFoldout, $"Bones {rig.bones.Length}(base: {rig.baseBoneCount})", true);
            if (!_bonesFoldout) {
                return;
            }

            using var bonesDisableScope = new EditorGUI.DisabledScope(true);
            ++EditorGUI.indentLevel;

            var height = EditorGUIUtility.singleLineHeight * (rig.boneNames.Length + 1);
            var tableRect = (PropertyDrawerRects)EditorGUILayout.GetControlRect(false, height);
            var headerRect = AllocateLine(ref tableRect);
            var (boneRect, nameRect, parentRect) = AllocateColumns(ref headerRect);

            EditorGUI.LabelField(boneRect, "Bone", EditorStyles.boldLabel);
            EditorGUI.LabelField(nameRect, "Name", EditorStyles.boldLabel);
            EditorGUI.LabelField(parentRect, "Parent", EditorStyles.boldLabel);

            for (int i = 0; i < rig.bones.Length; ++i) {
                var rowRect = AllocateLine(ref tableRect);
                (boneRect, nameRect, parentRect) = AllocateColumns(ref rowRect);
                // Bone
                EditorGUI.ObjectField(boneRect, rig.bones[i], typeof(Transform), true);
                // Name
                var boneName = rig.boneNames[i].ToString();
                EditorGUI.SelectableLabel(nameRect, boneName);
                // Parent
                var parentIndex = rig.boneParents[i];
                var parent = parentIndex switch {
                    ushort.MaxValue => null,
                    _ => rig.bones[parentIndex]
                };
                EditorGUI.ObjectField(parentRect, parent, typeof(Transform), true);
            }

            --EditorGUI.indentLevel;
        }

        void DrawDebug() {
            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "Debug", true);

            if (!_debugFoldout) {
                return;
            }

            ++EditorGUI.indentLevel;

            var rig = (KandraRig)target;

            using (new EditorGUI.DisabledScope(true)) {
                EditorGUILayout.Toggle("Is registered", KandraRig.EditorAccessor.IsRegistered(rig));

                _mergedRenderersList.DoLayoutList();
                _activeRenderersList.DoLayoutList();
            }

            if (GUILayout.Button("Validate Bones")) {
                ValidateBones(rig);
            }

            --EditorGUI.indentLevel;
        }

        void ValidateBones(KandraRig rig) {
            if (rig.bones.Length != rig.boneParents.Length) {
                SimplePopup("The number of bones does not match the number of parents. Please fix th rig");
            }
            if (rig.bones.Length != rig.boneNames.Length) {
                SimplePopup("The number of bones does not match the number of names. Please fix the rig");
            }

            if (!Application.isPlaying) {
                if (rig.baseBoneCount != rig.bones.Length) {
                    SimplePopup("The base bone count does not match the number of bones. Please fix the rig");
                }
            }

            for (int i = 0; i < rig.bones.Length; ++i) {
                var bone = rig.bones[i];
                if (bone == null) {
                    SimplePopup($"Bone {i} is null. Please fix the rig");
                }

                if (bone.name != rig.boneNames[i].ToString()) {
                    AutoFixupPopup($"Bone {i} name does not match the name in the names array. Please fix the rig");
                }

                var parentIndex = rig.boneParents[i];
                if (parentIndex != ushort.MaxValue) {
                    if (parentIndex >= rig.bones.Length) {
                        AutoFixupPopup($"Bone {i} parent index is out of bounds. Please fix the rig");
                    }
                    if (rig.bones[parentIndex] != bone.parent) {
                        AutoFixupPopup($"Bone {i} parent does not match the parent in the parents array. Please fix the rig");
                    }
                }
            }

            void SimplePopup(string message) {
                Selection.activeObject = rig;
                EditorGUIUtility.PingObject(rig);
                EditorUtility.DisplayDialog("Kandra Rig Error", message, "I'll fix it");
            }

            void AutoFixupPopup(string message) {
                Selection.activeObject = rig;
                EditorGUIUtility.PingObject(rig);
                if (EditorUtility.DisplayDialog("Kandra Rig Error", message, "Auto-fix", "I'll fix it")) {
                    FixupBones(rig);
                }
            }
        }

        void FixupBones(KandraRig rig) {
            if (Application.isPlaying) {
                return;
            }

            Undo.RegisterCompleteObjectUndo(rig, "Fixup Bones");

            for (int i = 0; i < rig.bones.Length; i++) {
                var bone = rig.bones[i];
                var parent = bone.parent;
                var parentIndex = unchecked((ushort)Array.IndexOf(rig.bones, parent));
                var fixedName = new FixedString64Bytes();
                fixedName.CopyFromTruncated(bone.name);

                rig.boneParents[i] = parentIndex;
                rig.boneNames[i] = fixedName;
            }

            EditorUtility.SetDirty(rig);
        }

        PropertyDrawerRects AllocateLine(ref PropertyDrawerRects rect) {
            return rect.AllocateTop(EditorGUIUtility.singleLineHeight);
        }

        (Rect, Rect, Rect) AllocateColumns(ref PropertyDrawerRects rect) {
            var width = rect.Rect.width;
            var boneColumn = width * 0.38f;
            var nameColumn = width * 0.20f;
            var parentColumn = width - boneColumn - nameColumn;

            var boneColumnRect = rect.AllocateLeftWithPadding(boneColumn, 2);
            var nameColumnRect = rect.AllocateLeftWithPadding(nameColumn, 2);
            var parentColumnRect = rect.AllocateLeftWithPadding(parentColumn, 2);
            return (boneColumnRect, nameColumnRect, parentColumnRect);
        }

        ReorderableList.ElementCallbackDelegate DrawKandraRendererElement(List<KandraRenderer> renderers) {
            return Draw;
            void Draw(Rect rect, int index, bool isActive, bool isFocused) {
                var kandraRenderer = renderers[index];
                EditorGUI.ObjectField(rect, kandraRenderer, typeof(KandraRenderer), true);
            }
        }
    }
}