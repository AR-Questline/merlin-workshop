using System;
using System.Linq;
using System.Text;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Editor.DrakeRenderer {
    [CustomEditor(typeof(DrakeMeshRenderer))]
    public class DrakeMeshRendererEditor : OdinEditor {
        static readonly StringBuilder MaskBuilder = new StringBuilder(64);

        Mesh _linkedMesh;
        [InlineEditor] Material[] _linkedMaterials;
        UnityEditor.Editor[] _materialEditors;
        bool[] _expandedMaterials;
        bool _debugExpanded;

        protected override void OnEnable() {
            base.OnEnable();
            var drakeMeshRenderer = (DrakeMeshRenderer)target;
            LoadUnityAssets(drakeMeshRenderer);
            OnValidate();
        }

        void OnValidate() {
            DrakeMeshRenderer drakeMeshRenderer = (DrakeMeshRenderer)target;
            var meshRenderer = drakeMeshRenderer.GetComponent<MeshRenderer>();
            if (!meshRenderer) {
                return;
            }
            var lod = meshRenderer.GetComponentInParent<LODGroup>();
            if (lod) {
                var lods = lod.GetLODs();
                var shouldBeInLod = lods.Any(l => l.renderers.Contains(meshRenderer));
                if (shouldBeInLod && !lod.TryGetComponent<DrakeLodGroup>(out _)) {
                    if (EditorUtility.DisplayDialog("DrakeRenderer", "LODGroup is not DrakeLodGroup",
                            "Add DrakeLodGroup", "Cancel")) {
                        var lodGo = lod.gameObject;
                        lodGo.AddComponent<DrakeLodGroup>();
                        Selection.activeGameObject = lodGo;
                    }
                }
            }
        }
        
        public override void OnInspectorGUI() {
            var drakeMeshRenderer = (DrakeMeshRenderer)target;
            var isEditable = PrefabsHelper.IsLowestEditablePrefabStage(drakeMeshRenderer);

            DrawAdditionalInspector(isEditable);

            if (drakeMeshRenderer.IsStatic != drakeMeshRenderer.gameObject.isStatic) {
                drakeMeshRenderer.BakeStatic();
                EditorUtility.SetDirty(drakeMeshRenderer);
            }

            if (!drakeMeshRenderer.IsBaked) {
                DrawDebugInspector();
                return;
            }

            using (var __ = new EditorGUI.DisabledGroupScope(!isEditable)) {
                MaskBuilder.Append("Used in lods:");
                for (int i = 0; i < 8; i++) {
                    if ((drakeMeshRenderer.LodMask & (1 << i)) == 0) {
                        continue;
                    }
                    MaskBuilder.Append(" ");
                    MaskBuilder.Append(i);
                }
                EditorGUILayout.LabelField(MaskBuilder.ToString());
                MaskBuilder.Clear();
                if (_linkedMesh) {
                    DrawMeshSetter(drakeMeshRenderer);
                }
                if (_linkedMaterials is { Length: > 0 }) {
                    DrawMaterialsSetter(drakeMeshRenderer);
                }
            }

            DrawDebugInspector();
        }

        void DrawAdditionalInspector(bool isEditable) {
            var drakeMeshRenderer = (DrakeMeshRenderer)target;
            
            using var _ = new EditorGUI.DisabledGroupScope(!isEditable);

            if (drakeMeshRenderer.IsBaked) {
                var meshRenderer = drakeMeshRenderer.GetComponent<MeshRenderer>();
                if (meshRenderer) {
                    EditorGUILayout.HelpBox("Baked DrakeMeshRenderer and MeshRenderer found.", MessageType.Error);
                }

                if (drakeMeshRenderer.Parent) {
                    return;
                }

                meshRenderer = drakeMeshRenderer.GetComponent<MeshRenderer>();
                if (meshRenderer) {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Remove MeshRenderer")) {
                        DestroyImmediate(meshRenderer);
                    }
                    if (GUILayout.Button("Remove DrakeMeshRenderer")) {
                        DestroyImmediate(drakeMeshRenderer);
                    }
                    EditorGUILayout.EndHorizontal();
                } else if (GUILayout.Button("Authoring mode")) {
                    LoadUnityAssets(drakeMeshRenderer);
                    Unbake(drakeMeshRenderer);
                    var gameObject = drakeMeshRenderer.gameObject;
                    DestroyImmediate(drakeMeshRenderer);
                    EditorUtility.SetDirty(gameObject);
                }
            } else {
                EditorGUILayout.HelpBox("DrakeMeshRenderer is not baked so it shouldn't be here", MessageType.Error);
                if (GUILayout.Button("Exchange DrakeMeshRenderer with DrakeToBake")) {
                    var gameObject = drakeMeshRenderer.gameObject;
                    DestroyImmediate(drakeMeshRenderer);
                    gameObject.AddComponent<DrakeToBake>();
                }
            }
        }

        void DrawDebugInspector() {
            GUILayout.Space(8);
            _debugExpanded = EditorGUILayout.Foldout(_debugExpanded, "Debug", true);
            if (!_debugExpanded) {
                return;
            }
            using var _ = new EditorGUI.DisabledGroupScope(true);

            base.OnInspectorGUI();

            var drakeMeshRenderer = (DrakeMeshRenderer)target;
            if (drakeMeshRenderer.IsBaked) {
                var bounds = drakeMeshRenderer.WorldBounds.ToBounds();
                GUILayout.Label($"Bounds:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                GUILayout.Label($"Center{bounds.center} Size{bounds.size}");
                GUILayout.Label($"Volume{bounds.Volume():f2} VolumeOfAverage{bounds.VolumeOfAverage():f2}");
                EditorGUI.indentLevel--;
            }
        }

        void DrawMeshSetter(DrakeMeshRenderer drakeMeshRenderer) {
            EditorGUI.BeginChangeCheck();
            var newMesh = EditorGUILayout.ObjectField("Mesh", _linkedMesh, typeof(Mesh), false) as Mesh;
            if (DrakeRendererEditorHelper.FindAssetEntry(drakeMeshRenderer.MeshReference.AssetGUID) == null) {
                DrawWarningAndFixButton(newMesh);
            }
            DrakeEditorHelpers.MeshStats(newMesh, out var vertexCount, out var triangleCount, out var subMeshCount);
            EditorGUILayout.LabelField($"{vertexCount} vert-{triangleCount} tris-{subMeshCount} submeshes");
            if (!EditorGUI.EndChangeCheck() || !newMesh || newMesh == _linkedMesh) {
                return;
            }
            var (settings, group) = DrakeRendererEditorHelper.GetInitialSetup(DrakeRendererEditorHelper.GroupName);
            
            if (!DrakeRendererEditorHelper.ProcessEntry(newMesh, settings, group, out var meshReference)) {
                return;
            }
            var materialReferences = drakeMeshRenderer.MaterialReferences;
            if (_linkedMesh.subMeshCount != newMesh.subMeshCount) {
                Array.Resize(ref _linkedMaterials, newMesh.subMeshCount);
                Array.Resize(ref _materialEditors, newMesh.subMeshCount);
                Array.Resize(ref _expandedMaterials, newMesh.subMeshCount);
                Array.Resize(ref materialReferences, newMesh.subMeshCount);
                if (newMesh.subMeshCount > _linkedMesh.subMeshCount) {
                    for (var i = _linkedMesh.subMeshCount; i < newMesh.subMeshCount; i++) {
                        _linkedMaterials[i] = _linkedMaterials[_linkedMesh.subMeshCount - 1];
                        materialReferences[i] = materialReferences[_linkedMesh.subMeshCount - 1];
                        _materialEditors[i] = _materialEditors[_linkedMesh.subMeshCount - 1];
                    }
                }
            }
            _linkedMesh = newMesh;
            drakeMeshRenderer.EDITOR_SetMeshReference(meshReference, newMesh);
            drakeMeshRenderer.EDITOR_SetMaterialsReferences(materialReferences, _linkedMaterials);
            EditorUtility.SetDirty(drakeMeshRenderer);
        }
        
        void DrawWarningAndFixButton(Object asset) {
            // SirenixEditorGUI.WarningMessageBox("Referenced mesh must be marked as Addressable Asset!");
            // if (SirenixEditorGUI.SDFIconButton(new GUIContent("Fix now", $"Mark the mesh asset as addressable, and put it into {DrakeRendererEditorHelper.GroupName} group."), 30, null)) {
            //     var (settings, group) = DrakeRendererEditorHelper.GetInitialSetup(DrakeRendererEditorHelper.GroupName);
            //     if (!DrakeRendererEditorHelper.ProcessEntry(asset, settings, group, out _)) {
            //         Log.Minor?.Error($"Failed to process entry for {asset}");
            //     }
            // }
        }

        void DrawMaterialsSetter(DrakeMeshRenderer drakeMeshRenderer) {
            EditorGUILayout.LabelField("Materials:");
            ++EditorGUI.indentLevel;
            for (var i = 0; i < _linkedMaterials.Length; i++) {
                DrawMaterialSetter(drakeMeshRenderer, i);
            }
            --EditorGUI.indentLevel;
        }

        void DrawMaterialSetter(DrakeMeshRenderer drakeMeshRenderer, int i) {
            EditorGUI.BeginChangeCheck();
            var rect = EditorGUILayout.BeginHorizontal();

            rect = EditorGUI.IndentedRect(rect);
            _expandedMaterials[i] = EditorGUI.Foldout(rect, _expandedMaterials[i], $"Mat.{i}");
            EditorGUILayout.Space(rect.width);

            var newMaterial = EditorGUILayout.ObjectField(_linkedMaterials[i], typeof(Material), false) as Material;

            var wasChanged = EditorGUI.EndChangeCheck();
            EditorGUILayout.EndHorizontal();
            if (_materialEditors[i] == null) {
                Log.Debug?.Error($"Material is null on {nameof(DrakeMeshRenderer)} on gameObject: {drakeMeshRenderer.gameObject}");
                return;
            }
            InternalEditorUtility.SetIsInspectorExpanded(_materialEditors[i].target, _expandedMaterials[i]);
            if (_expandedMaterials[i]) {
                _materialEditors[i].OnInspectorGUI();
            }
            if (!wasChanged || !newMaterial || newMaterial == _linkedMaterials[i]) {
                return;
            }

            var (settings, group) = DrakeRendererEditorHelper.GetInitialSetup(DrakeRendererEditorHelper.GroupName);
            if (!DrakeRendererEditorHelper.ProcessEntry(newMaterial, settings, group, out var materialReference)) {
                return;
            }
            var materialReferences = drakeMeshRenderer.MaterialReferences;

            materialReferences[i] = materialReference;
            _linkedMaterials[i] = newMaterial;

            drakeMeshRenderer.EDITOR_SetMaterialsReferences(materialReferences, _linkedMaterials);
            EditorUtility.SetDirty(drakeMeshRenderer);
        }

        public static bool Bake(DrakeMeshRenderer drakeMeshRenderer, MeshRenderer meshRenderer) {
            var (settings, group) = DrakeRendererEditorHelper.GetInitialSetup(DrakeRendererEditorHelper.GroupName);
            if (DrakeRendererEditorHelper.SetupRenderer(drakeMeshRenderer, meshRenderer, settings, group)) {
                drakeMeshRenderer.BakeStatic();
                EditorUtility.SetDirty(drakeMeshRenderer);
                return true;
            }
            return false;
        }

        public static MeshRenderer Unbake(DrakeMeshRenderer drakeRenderer, bool? isStatic = null) {
            MeshRenderer meshRenderer = SpawnAuthoring(drakeRenderer, isStatic: isStatic);
            Undo.RegisterCompleteObjectUndo(drakeRenderer, "Unbake DrakeMeshRenderer");
            var descriptor = drakeRenderer.RenderMeshDescription(false); // Unbake as it was baked
            meshRenderer.renderingLayerMask = descriptor.FilterSettings.RenderingLayerMask;
            meshRenderer.shadowCastingMode = descriptor.FilterSettings.ShadowCastingMode;
            meshRenderer.receiveShadows = descriptor.FilterSettings.ReceiveShadows;
            meshRenderer.motionVectorGenerationMode = descriptor.FilterSettings.MotionMode;
            meshRenderer.staticShadowCaster = descriptor.FilterSettings.StaticShadowCaster;
            meshRenderer.lightProbeUsage = descriptor.LightProbeUsage;
            drakeRenderer.ClearData();
            DrakeMeshRenderer.OnRemovedDrakeMeshRenderer?.Invoke(drakeRenderer);
            EditorUtility.SetDirty(drakeRenderer);
            return meshRenderer;
        }

        public static MeshRenderer SpawnAuthoring(DrakeMeshRenderer drakeRenderer, GameObject parentUnbakeTarget = null, bool? isStatic = null) {
            Mesh mesh;
            Material[] materials;

            using (var preview = new DrakeMeshRendererPreview(drakeRenderer)) {
                mesh = preview.Mesh;
                materials = preview.Materials.ToArray();
            }

            if (!mesh) {
                Log.Critical?.Error($"Mesh is null for drakeRenderer {drakeRenderer.name}", drakeRenderer);
                return null;
            }
            if (materials.Any(static m => !m)) {
                Log.Critical?.Error($"Material is null for drakeRenderer {drakeRenderer.name}", drakeRenderer);
            }

            var go = drakeRenderer.gameObject;
            if (drakeRenderer.Parent) {
                // Hierarchy was flatten so expand it
                if (drakeRenderer.Parent.gameObject == go) {
                    go = new GameObject($"{mesh.name}");
                    go.isStatic = isStatic ?? drakeRenderer.IsStatic;
                    // This is temporary authoring view so we don't want to save it
                    if (parentUnbakeTarget) {
                        go.hideFlags = HideFlags.DontSaveInBuild |
                                       HideFlags.DontSaveInEditor;
                    }
                    var newTransform = go.transform;
                    var drakeMatrix = drakeRenderer.LocalToWorld;
                    newTransform.localPosition = mathExt.Translation(drakeMatrix);
                    newTransform.localRotation = mathExt.Rotation(drakeMatrix);
                    newTransform.localScale = mathExt.Scale(drakeMatrix);
                    var parentTransform = parentUnbakeTarget ? parentUnbakeTarget.transform : drakeRenderer.Parent.transform;
                    newTransform.SetParent(parentTransform, true);
                }
                // Hierarchy wasn't flatten but we need to move it to the temporal parent
                else if (parentUnbakeTarget) {
                    var oldTransform = go.transform;
                    go = new GameObject($"{mesh.name}");
                    go.isStatic = isStatic ?? drakeRenderer.IsStatic;
                    go.hideFlags = HideFlags.DontSaveInBuild |
                                   HideFlags.DontSaveInEditor;
                    var newTransform = go.transform;
                    newTransform.SetParent(parentUnbakeTarget.transform, false);
                    newTransform.position = oldTransform.position;
                    newTransform.rotation = oldTransform.rotation;
                    newTransform.localScale = oldTransform.localScale;
                }
            } else if (parentUnbakeTarget) {
                go = parentUnbakeTarget;
            }
            go.layer = drakeRenderer.RenderMeshDescription(isStatic ?? drakeRenderer.IsStatic).FilterSettings.Layer;
            MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = go.AddComponent<MeshFilter>();

            meshRenderer.sharedMaterials = materials;
            meshFilter.sharedMesh = mesh;
            return meshRenderer;
        }

        void LoadUnityAssets(DrakeMeshRenderer drakeMeshRenderer) {
            if (!drakeMeshRenderer.IsBaked) {
                return;
            }
            _linkedMesh = DrakeEditorHelpers.LoadAsset<Mesh>(drakeMeshRenderer.MeshReference);
            _linkedMaterials = drakeMeshRenderer.MaterialReferences.Select(DrakeEditorHelpers.LoadAsset<Material>).ToArray();
            _materialEditors = _linkedMaterials.Select(UnityEditor.Editor.CreateEditor).ToArray();
            _expandedMaterials = new bool[_linkedMaterials.Length];
        }
    }
}
