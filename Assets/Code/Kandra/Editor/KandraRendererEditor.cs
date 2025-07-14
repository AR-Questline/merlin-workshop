using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Kandra.Data;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Editor {
    [CustomEditor(typeof(KandraRenderer))]
    public class KandraRendererEditor : UnityEditor.Editor {
        SerializedProperty _rendererDataProperty;

        SerializedProperty _rigProperty;

        bool _meshFoldout = true;
        bool _meshDebugFoldout;
        SerializedProperty _meshProperty;
        SerializedProperty _editorMeshProperty;
        SerializedProperty _boundsAmplifierProperty;

        bool _materialsFoldout = true;
        bool _materialsDebugFoldout;
        List<Material> _editorMaterials = new();
        ReorderableList _materialsReorderableList;

        bool _blendshapesFoldout;
        SerializedProperty _constantBlendshapesProperty;

        bool _filteringSettingsFoldout;

        bool _bonesFoldout;
        SerializedProperty _bonesProperty;
        ReorderableList _bonesReorderableList;
        SerializedProperty _rootBoneProperty;
        SerializedProperty _rootBoneMatrixProperty;

        bool _memoryInfoFoldout;

        bool _stateInfoFoldout;

        OnDemandCache<Material, UnityEditor.Editor> _materialEditors = new(CreateEditor);

        bool _showSkinnedVertices;

        void OnEnable() {
            // Main
            _rendererDataProperty = serializedObject.FindProperty(nameof(KandraRenderer.rendererData));
            // Rig
            _rigProperty = _rendererDataProperty.FindPropertyRelative(nameof(KandraRenderer.RendererData.rig));
            // Mesh
            _meshProperty = _rendererDataProperty.FindPropertyRelative(nameof(KandraRenderer.RendererData.mesh));
            _editorMeshProperty = _rendererDataProperty.FindPropertyRelative(nameof(KandraRenderer.RendererData.EDITOR_sourceMesh));
            _boundsAmplifierProperty = _rendererDataProperty.FindPropertyRelative(nameof(KandraRenderer.RendererData.boundsAmplifier));
            // Materials
            _editorMaterials.AddRange(((KandraRenderer)target).rendererData.materials);
            _materialsReorderableList = new ReorderableList(_editorMaterials, typeof(Material));

            _materialsReorderableList.drawHeaderCallback = rect => {
                EditorGUI.LabelField(rect, "Materials");
            };
            _materialsReorderableList.drawElementCallback = (rect, index, isActive, isFocused) => {
                var material = _editorMaterials[index];
                _editorMaterials[index] = (Material)EditorGUI.ObjectField(rect, material, typeof(Material), false);
            };
            // Blendshapes
            _constantBlendshapesProperty = _rendererDataProperty.FindPropertyRelative(nameof(KandraRenderer.RendererData.constantBlendshapes));
            // Bones
            _bonesProperty = _rendererDataProperty.FindPropertyRelative(nameof(KandraRenderer.RendererData.bones));
            _rootBoneProperty = _rendererDataProperty.FindPropertyRelative(nameof(KandraRenderer.RendererData.rootBone));
            _rootBoneMatrixProperty = _rendererDataProperty.FindPropertyRelative(nameof(KandraRenderer.RendererData.rootBoneMatrix));

            _bonesReorderableList = new ReorderableList(serializedObject, _bonesProperty, false, true, false, false);
            _bonesReorderableList.drawHeaderCallback = rect => {
                EditorGUI.LabelField(rect, $"Bones ({_bonesProperty.arraySize})");
            };
            _bonesReorderableList.drawElementCallback = (rect, index, isActive, isFocused) => {
                var rig = (KandraRig)_rigProperty.objectReferenceValue;
                var propertyRect = (PropertyDrawerRects)rect;
                var boneIndexRect = propertyRect.AllocateLeftNormalized(0.49f);
                var boneTransformRect = (Rect)propertyRect;

                var boneProp = _bonesProperty.GetArrayElementAtIndex(index);
                var boneIndex = boneProp.intValue;

                EditorGUI.IntField(boneIndexRect, boneIndex);
                if (rig) {
                    var boneTransform = rig.bones[boneIndex];
                    EditorGUI.ObjectField(boneTransformRect, boneTransform, typeof(Transform), true);
                } else {
                    EditorGUI.LabelField(boneTransformRect, "No Rig");
                }
            };
        }

        void OnDisable() {
            foreach (var (material, editor) in _materialEditors) {
                DestroyImmediate(editor);
            }
            _materialEditors.Clear();
        }
        
        public override void OnInspectorGUI() {
            var renderer = (KandraRenderer)target;

            EditorGUILayout.PropertyField(_rigProperty, true);

            DrawMeshSection();

            DrawMaterialsSection();

            DrawBlendshapesSection();

            DrawRendererSettingsSection();

            DrawBonesSection();

            _memoryInfoFoldout = EditorGUILayout.Foldout(_memoryInfoFoldout, "Memory info", true);
            if (_memoryInfoFoldout) {
                ++EditorGUI.indentLevel;
                renderer.DrawMemoryInfo();
                --EditorGUI.indentLevel;
            }

            _stateInfoFoldout = EditorGUILayout.Foldout(_stateInfoFoldout, "State info", true);
            if (_stateInfoFoldout) {
                using var stateDisable = new EditorGUI.DisabledGroupScope(true);
                ++EditorGUI.indentLevel;
                EditorGUILayout.LongField("Rendering ID", renderer.RenderingId & KandraRendererManager.ValidBitmask);
                EditorGUILayout.Toggle("Is Registered", KandraRendererManager.Instance.IsRegistered(renderer.RenderingId));
                EditorGUILayout.Toggle("Is Waiting", KandraRendererManager.IsWaitingId(renderer.RenderingId));
                EditorGUILayout.Toggle("Is Invalid", KandraRendererManager.IsInvalidId(renderer.RenderingId));
                --EditorGUI.indentLevel;
            }

            var originalMaterials = renderer.rendererData.materials;
            foreach (var originalMaterial in originalMaterials) {
                var materialEditor = _materialEditors[originalMaterial];
                UnityEditor.Editor.DrawFoldoutInspector(originalMaterial, ref materialEditor);
                _materialEditors[originalMaterial] = materialEditor;
            }
        }

        void DrawMeshSection() {
            serializedObject.UpdateIfRequiredOrScript();

            var meshRect = default(Rect);

            _meshFoldout = EditorGUILayout.Foldout(_meshFoldout, "Mesh", true);
            if (!_meshFoldout) {
                return;
            }

            ++EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledGroupScope(true)) {
                EditorGUILayout.PropertyField(_meshProperty, true);
                meshRect = GUILayoutUtility.GetLastRect();
            }
            if (GUILayout.Button("Replace", GUILayout.Width(64))) {
                KandraMeshReplacer.Replace((KandraRenderer)target);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_boundsAmplifierProperty, true);
            serializedObject.ApplyModifiedProperties();

            _meshDebugFoldout = EditorGUILayout.Foldout(_meshDebugFoldout, "Debug", true);
            if (_meshDebugFoldout) {
                ++EditorGUI.indentLevel;
                using (new EditorGUI.DisabledGroupScope(true)) {
                    EditorGUILayout.PropertyField(_editorMeshProperty, true);
                }

                var rendererData = ((KandraRenderer)target).rendererData;

                DrawRenderingMesh("Original Mesh", rendererData.originalMesh);
                DrawRenderingMesh("Culled Mesh", rendererData.culledMesh);
                --EditorGUI.indentLevel;
            }

            --EditorGUI.indentLevel;

            Event currentEvent = Event.current;
            if (meshRect.Contains(currentEvent.mousePosition)) {
                if (currentEvent.type == EventType.DragUpdated) {
                    if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is Mesh) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        currentEvent.Use();
                    }
                } else if (currentEvent.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();
                    KandraMeshReplacer.Replace((KandraRenderer)target, DragAndDrop.objectReferences[0] as Mesh);
                    currentEvent.Use();
                }
            }

            void DrawRenderingMesh(string name, KandraRenderingMesh renderingMesh) {
                EditorGUILayout.BeginVertical("box");

                if (!renderingMesh.IsValid) {
                    EditorGUILayout.LabelField($"{name} - invalid.", EditorStyles.boldLabel);
                } else {

                    EditorGUILayout.LabelField(name, EditorStyles.boldLabel);

                    using EditorGUI.DisabledGroupScope disabledGroupScope = new EditorGUI.DisabledGroupScope(true);
                    EditorGUILayout.LongField("Index Start", renderingMesh.indexStart);

                    EditorGUILayout.LabelField($"Submeshes: {renderingMesh.submeshes.Length}");

                    ++EditorGUI.indentLevel;
                    for (var i = 0u; i < renderingMesh.submeshes.Length; i++) {
                        var submesh = renderingMesh.submeshes[i];
                        EditorGUILayout.LabelField($"Submesh {i}: {submesh}");
                    }
                    --EditorGUI.indentLevel;
                }
                EditorGUILayout.EndVertical();
            }
        }

        void DrawMaterialsSection() {
            _materialsFoldout = EditorGUILayout.Foldout(_materialsFoldout, "Materials", true);
            if (!_materialsFoldout) {
                return;
            }
            ++EditorGUI.indentLevel;

            var renderer = (KandraRenderer)target;
            ref var rendererData = ref renderer.rendererData;
            var materials = rendererData.materials;
            _editorMaterials.Clear();
            _editorMaterials.AddRange(materials);

            var disableMaterialEditing = rendererData.materialsInstancesRefCount.IsNotNullOrEmpty() && rendererData.materialsInstancesRefCount.Any(static r => r > 0);
            using (new EditorGUI.DisabledGroupScope(disableMaterialEditing)) {
                EditorGUI.BeginChangeCheck();

                _materialsReorderableList.DoLayoutList();

                if (EditorGUI.EndChangeCheck()) {
                    renderer.EDITOR_ClearMaterials();
                    rendererData.materials = _editorMaterials.ToArray();
                    renderer.EDITOR_RecreateMaterials();
                }
            }

            _materialsDebugFoldout = EditorGUILayout.Foldout(_materialsDebugFoldout, "Debug", true);
            if (_materialsDebugFoldout) {
                using var materialsDebugDisable = new EditorGUI.DisabledGroupScope(true);
                ++EditorGUI.indentLevel;
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Originals");
                foreach (var material in rendererData.materials) {
                    EditorGUILayout.ObjectField(material, typeof(Material), false);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(72));
                EditorGUILayout.LabelField("RefCounts", GUILayout.Width(72));
                foreach (var refCount in rendererData.materialsInstancesRefCount) {
                    EditorGUILayout.LabelField(refCount.ToString(), GUILayout.Width(72));
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Instances");
                foreach (var material in rendererData.materialsInstances) {
                    EditorGUILayout.ObjectField(material, typeof(Material), false);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                --EditorGUI.indentLevel;
            }

            --EditorGUI.indentLevel;
        }

        void DrawBonesSection() {
            _bonesFoldout = EditorGUILayout.Foldout(_bonesFoldout, "Bones", true);
            if (!_bonesFoldout) {
                return;
            }

            var rig = (KandraRig)_rigProperty.objectReferenceValue;

            using var bonesDisable = new EditorGUI.DisabledGroupScope(true);
            ++EditorGUI.indentLevel;

            _bonesReorderableList.DoLayoutList();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.IntField("Root bone", _rootBoneProperty.intValue);
            if (rig) {
                var rootBone = rig.bones[_rootBoneProperty.intValue];
                EditorGUILayout.ObjectField(rootBone, typeof(Transform), true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_rootBoneMatrixProperty, true);

            --EditorGUI.indentLevel;
        }

        void DrawBlendshapesSection() {
            _blendshapesFoldout = EditorGUILayout.Foldout(_blendshapesFoldout, "Blendshapes", true);
            if (!_blendshapesFoldout) {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.PropertyField(_constantBlendshapesProperty, true);
            serializedObject.ApplyModifiedProperties();

            var renderer = (KandraRenderer)target;
            var rendererData = renderer.rendererData;

            if (!rendererData.blendshapeWeights.IsCreated || rendererData.mesh.blendshapesNames.Length <= 0) {
                return;
            }

            ++EditorGUI.indentLevel;
            for (var i = 0u; i < rendererData.blendshapeWeights.Length; i++) {
                var weight = rendererData.blendshapeWeights[i];
                EditorGUI.BeginChangeCheck();
                var newWeight = EditorGUILayout.Slider(rendererData.mesh.blendshapesNames[i], weight, 0, 1);
                if (EditorGUI.EndChangeCheck()) {
                    rendererData.blendshapeWeights[i] = newWeight;
                    if (!Application.isPlaying && SceneView.lastActiveSceneView != null) {
                        SceneView.lastActiveSceneView.Repaint();
                    }
                }
            }

            --EditorGUI.indentLevel;
        }

        void DrawRendererSettingsSection() {
            _filteringSettingsFoldout = EditorGUILayout.Foldout(_filteringSettingsFoldout, "Filtering Settings", true);
            if (!_filteringSettingsFoldout) {
                return;
            }

            var renderer = (KandraRenderer)target;
            ref var rendererData = ref renderer.rendererData;

            ref var filteringSettings = ref rendererData.filteringSettings;

            ++EditorGUI.indentLevel;

            filteringSettings.shadowCastingMode = (ShadowCastingMode)EditorGUILayout.EnumPopup("Shadow Casting Mode", filteringSettings.shadowCastingMode);
            var layerMaskValue = (KandraRenderingLayers)filteringSettings.renderingLayersMask;
            filteringSettings.renderingLayersMask = (uint)(KandraRenderingLayers)EditorGUILayout.EnumFlagsField("Rendering Layers Mask", layerMaskValue);

            --EditorGUI.indentLevel;
        }

        void OnSceneGUI() {
            var renderer = (KandraRenderer)target;

            if (KandraRendererManager.IsInvalidId(renderer.RenderingId) || KandraRendererManager.IsWaitingId(renderer.RenderingId)) {
                return;
            }

            if (_showSkinnedVertices) {
                var rendererData = renderer.rendererData;

                var oldColor = Handles.color;
                Handles.color = Color.blue;
                var skinningManager = KandraRendererManager.Instance.SkinningManager;
                var startingIndex = skinningManager.GetVertexStart(renderer.RenderingId);

                var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(rendererData.mesh);
                var trianglesCount = indices.Length / 3;
                var skinnedVertices = new CompressedVertex[rendererData.mesh.vertexCount];
                skinningManager.OutputVerticesBuffer.GetData(skinnedVertices, 0, (int)startingIndex, rendererData.mesh.vertexCount);

                for (var i = 0u; i < trianglesCount; ++i) {
                    var i1 = indices[i * 3];
                    var i2 = indices[i * 3 + 1];
                    var i3 = indices[i * 3 + 2];

                    Handles.DrawLine(skinnedVertices[i1].position, skinnedVertices[i2].position);
                    Handles.DrawLine(skinnedVertices[i2].position, skinnedVertices[i3].position);
                    Handles.DrawLine(skinnedVertices[i3].position, skinnedVertices[i1].position);
                }
                Handles.color = oldColor;
            }

            if (EditorPrefs.GetBool("showKandraSkinnedVertices", false)) {
                KandraRendererManager.Instance.GetBoundsAndRootBone(renderer.RenderingId, out var worldBoundingSphere, out _);

                var rect = HandlesUtils.LabelRect(new Vector3(worldBoundingSphere.x, worldBoundingSphere.y, worldBoundingSphere.z), "Show Skinned Vertices", EditorStyles.toggle);
                Handles.BeginGUI();
                EditorGUI.DrawRect(rect, Color.black);
                _showSkinnedVertices = GUI.Toggle(rect, _showSkinnedVertices, "Show Skinned Vertices", EditorStyles.toggle);
                Handles.EndGUI();
            }
        }
    }

    [Flags]
    public enum KandraRenderingLayers : uint {
        Default = 1 << 0,
        UI = 1 << 1,
        EnvironmentUI = 1 << 2,
        LightLayer3 = 1 << 3,
        LightLayer4 = 1 << 4,
        LightLayer5 = 1 << 5,
        LightLayer6 = 1 << 6,
        LightLayer7 = 1 << 7,
        DecalLayerDefault = 1 << 8,
        DecalLayer1 = 1 << 9,
        DecalLayer2 = 1 << 10,
        DecalLayer3 = 1 << 11,
        DecalLayer4 = 1 << 12,
        DecalLayer5 = 1 << 13,
        DecalLayerItems = 1 << 14,
        DecalLayerAI = 1 << 15,
    }
}