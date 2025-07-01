using System;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Files;
using Awaken.Utility.Graphics;
using Awaken.Utility.UI;
using QFSW.QC;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Awaken.ECS.MedusaRenderer {
    public class MedusaDebugWindow : UGUIWindowDisplay<MedusaDebugWindow> {
        protected override void DrawWindow() {
            var medusa = FindAnyObjectByType<MedusaRendererManager>(FindObjectsInactive.Include);
            if (medusa == null) {
                GUILayout.Label("MedusaRendererManager not found");
                return;
            }
            if (medusa.enabled && GUILayout.Button("To unity")) {
                var access = new MedusaRendererManager.EditorAccess(medusa);
                ToUnityRenderers(access);
            }
            if (!medusa.enabled && GUILayout.Button("To brg")) {
                var access = new MedusaRendererManager.EditorAccess(medusa);
                ToMedusa(access);
            }
        }

        static void ToUnityRenderers(MedusaRendererManager.EditorAccess access) {
            var brg = access.BrgRenderer;

            var matricesPath = MedusaPersistence.MatricesPath(brg.BasePath);
            var allMatrices = FileRead.ToNewBuffer<PackedMatrix>(matricesPath, ARAlloc.Temp);

            var matrices = allMatrices.AsSlice(0u, (uint)access.TransformsCount);
            var createdLods = new LODGroup[matrices.LengthInt];
            var allLods = new LOD[matrices.LengthInt][];

            var lastLodMasks = brg.LastLodMasks;
            for (var i = 0u; i < matrices.Length; i++) {
                var lodGo = new GameObject($"LOD_{i}", typeof(LODGroup));
                var lodGroup = lodGo.GetComponent<LODGroup>();
                createdLods[i] = lodGroup;

                var transform = lodGo.transform;
                transform.SetParent(access.Manager.transform, false);
                var fullMatrix = matrices[i].ToFloat4x4();
                transform.SetPositionAndRotation(fullMatrix.TransformPoint(float3.zero), fullMatrix.Rotation());
                var transformScale = fullMatrix.Scale();
                transform.localScale = transformScale;
                var lodScale = math.cmax(math.abs(transformScale));

                var lastLodMask = lastLodMasks[i];
                var lodsCount = (32 - math.lzcnt((int)lastLodMask)) + 1;
                ref var lods = ref allLods[i];
                lods = new LOD[lodsCount];
                for (byte j = 0; j < lodsCount; j++) {
                    var distance = math.sqrt(brg.LodDistanceSq(i, j));

                    var lod = new LOD(lodScale / distance, Array.Empty<UnityEngine.Renderer>());
                    lods[j] = lod;
                }
            }

            for (var rednererIndexer = 0u; rednererIndexer < access.Renderers.Length; rednererIndexer++) {
                var renderer = access.Renderers[rednererIndexer];
                var transformIndices = brg.TransformIndices(rednererIndexer);

                var mesh = renderer.renderData[0].mesh;
                var materials = new Material[renderer.renderData.Count];
                for (var j = 0u; j < renderer.renderData.Count; j++) {
                    materials[j] = renderer.renderData[(int)j].material;
                }

                for (var transformIndexer = 0u; transformIndexer < transformIndices.Length; transformIndexer++) {
                    var transformIndex = transformIndices[transformIndexer];
                    var lodGroup = createdLods[transformIndex];

                    var lodRendererGameObject = new GameObject("Renderer", typeof(MeshRenderer), typeof(MeshFilter));
                    var lodRendererTransform = lodRendererGameObject.transform;
                    lodRendererTransform.SetParent(lodGroup.transform, false);
                    var meshFilter = lodRendererGameObject.GetComponent<MeshFilter>();
                    meshFilter.sharedMesh = mesh;

                    var meshRenderer = lodRendererGameObject.GetComponent<MeshRenderer>();
                    meshRenderer.sharedMaterials = materials;

                    ref var lods = ref allLods[transformIndex];

                    for (var lodIndex = 0u; lodIndex < lods.Length; lodIndex++) {
                        if ((renderer.lodMask & (1 << (int)lodIndex)) == 0) {
                            continue;
                        }
                        ref var lod = ref lods[lodIndex];
                        Array.Resize(ref lod.renderers, lod.renderers.Length + 1);
                        lod.renderers[^1] = meshRenderer;
                    }
                }
            }

            for (var i = 0u; i < matrices.Length; i++) {
                createdLods[i].SetLODs(allLods[i]);
            }

            access.Manager.enabled = false;
            allMatrices.Dispose();
        }

        static void ToMedusa(MedusaRendererManager.EditorAccess access) {
            var transform = access.Manager.transform;
            var childrenCount = transform.childCount;
            for (int i = childrenCount - 1; i >= 0; i--) {
                Destroy(transform.GetChild(i).gameObject);
            }
            access.Manager.enabled = true;
        }

        [Command("rendering.medusa-to-unity", "Changes medusa rendering to unity rendering")][UnityEngine.Scripting.Preserve]
        static void MedusaToUnityRenderers() {
            var medusa = FindAnyObjectByType<MedusaRendererManager>(FindObjectsInactive.Include);
            if (medusa == null) {
                return;
            }
            var access = new MedusaRendererManager.EditorAccess(medusa);
            ToUnityRenderers(access);
        }

        [Command("rendering.medusa-to-medusa", "Changes medusa unity rendering to medusa rendering")][UnityEngine.Scripting.Preserve]
        static void MedusaToMedusa() {
            var medusa = FindAnyObjectByType<MedusaRendererManager>(FindObjectsInactive.Include);
            if (medusa == null) {
                return;
            }
            var access = new MedusaRendererManager.EditorAccess(medusa);
            ToMedusa(access);
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowMedusaDebug() {
            MedusaDebugWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }

        static bool IsDebugWindowShown() => MedusaDebugWindow.IsShown;
    }
}