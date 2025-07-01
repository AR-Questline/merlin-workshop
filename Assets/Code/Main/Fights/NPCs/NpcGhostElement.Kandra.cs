using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.VFX;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Fights.NPCs {
    /// <summary>
    /// Turns NPC into ghost, Kandra implementation
    /// </summary>
#pragma warning disable AR0026
    public partial class NpcGhostElement {
#pragma warning restore AR0026
        List<Material> _allKandraGhostMaterials = new();
        List<CancellationTokenSource> _ctsList;
        
        void ConvertRenderersToGhost(IEnumerable<KandraRenderer> kandraRenderers) {
            _allKandraGhostMaterials ??= new List<Material>();

            RenderersMarkers renderersMarkers = null;
            foreach (KandraRenderer renderer in kandraRenderers) {
                DissolveAbleRenderer dar = renderer.gameObject.GetComponent<DissolveAbleRenderer>();
                if (_revertable) {
                    dar ??= renderer.gameObject.AddComponent<DissolveAbleRenderer>();
                    dar.Init();
                }

                // Use default materials for NPC heads
                renderersMarkers ??= renderer.gameObject.GetComponentInParent<RenderersMarkers>();
                var headMarker = FindHeadMarker(renderer, renderersMarkers);

                if (headMarker != null) {
                    LoadDefaultHumanHeadGhostMaterials(renderer, dar).Forget();
                } else {
                    // Use custom materials
                    if (dar?.hasCustomGhostMaterials ?? false) {
                        foreach (var material in dar.dissolveAbleMaterials) {
                            material.SetFloat(Transition, _instant ? 1 : 0);
                        }

                        ApplyMaterials(renderer, dar, dar.dissolveAbleMaterials);
                        continue;
                    }

                    // Create materials
                    ApplyMaterials(renderer, dar, CreateNewMaterials(renderer));
                }
            }
        }

        RenderersMarkers.KandraMarker? FindHeadMarker(KandraRenderer renderer, RenderersMarkers renderersMarkers) {
            if (renderersMarkers != null) {
                foreach (var marker in renderersMarkers.KandraMarkers) {
                    if (marker.MaterialType.HasFlagFast(RendererMarkerMaterialType.Face) && marker.Renderer == renderer) {
                        return marker;
                    }
                }
            }

            return null;
        }

        Material[] CreateNewMaterials(KandraRenderer meshRenderer) {
            Material[] materials = meshRenderer.GetOriginalMaterials().CreateCopy();
            for (int i = 0; i < materials.Length; i++) {
                materials[i] = CreateNewMaterial(materials[i]);
            }
            return materials;
        }

        async UniTaskVoid LoadDefaultHumanHeadGhostMaterials(KandraRenderer renderer, DissolveAbleRenderer dar) {
            var references = Services.Get<CommonReferences>().defaultGhostHeadMaterials;
            var handles = new ARAsyncOperationHandle<Material>[references.Length];
            bool anyEmpty = false;
            for (int i = 0; i < references.Length; i++) {
                if (references[i] is { IsSet: true }) {
                    handles[i] = references[i].Get().LoadAsset<Material>();
                } else {
                    handles[i] = new ARAsyncOperationHandle<Material>();
                    anyEmpty = true;
                }
            }

            _ctsList ??= new List<CancellationTokenSource>();
            var cts = new CancellationTokenSource();
            _ctsList.Add(cts);
            if (!await AsyncUtil.WaitUntil(this, () => handles.All(h => !h.IsValid() || h.Status != AsyncOperationStatus.None), cts.Token)) {
                return;
            }
            _ctsList.Remove(cts);
            
            var originalMaterials = renderer.GetOriginalMaterials();
            var materials = anyEmpty ? originalMaterials.CreateCopy() : new Material[references.Length];
            int materialsMaxIndex = originalMaterials.Length;
            if (materialsMaxIndex != materials.Length) {
                Log.Minor?.Error($"Default ghost head materials count mismatch. Expected {materials.Length} got {materialsMaxIndex}. for {renderer.gameObject.name} in {renderer.gameObject.PathInSceneHierarchy()}");
                materialsMaxIndex = Mathf.Min(materials.Length, originalMaterials.Length);
            }
            for (int i = 0; i < materialsMaxIndex; i++) {
                if (handles[i].IsValid()) {
                    var newMaterial = Object.Instantiate(handles[i].Result);
                    CopyTextures(originalMaterials[i], newMaterial);
                    newMaterial.SetFloat(Transition, _instant ? 1 : 0);
                    materials[i] = newMaterial;
                } else {
                    materials[i] = CreateNewMaterial(materials[i]);
                }
            }

            ApplyMaterials(renderer, dar, materials);
        }

        void ApplyMaterials(KandraRenderer meshRenderer, DissolveAbleRenderer dar, Material[] materials) {
            if (_revertable) {
                dar.SetCustomDissolveAbleMaterials(materials, null, true);
                dar.ChangeToDissolveAble();
            } else {
                meshRenderer.ChangeOriginalMaterials(materials);
            }
                
            _allKandraGhostMaterials.AddRange(meshRenderer.rendererData.RenderingMaterials);
        }

        void RemoveGhostRenderers(IEnumerable<KandraRenderer> kandraRenderers) {
            foreach (var renderer in kandraRenderers) {
                foreach (var material in renderer.GetInstantiatedMaterials()) {
                    _allKandraGhostMaterials.Remove(material);
                }
            }
        }
        
        void UpdateKandraMaterials(float percent) {
            foreach (var material in _allKandraGhostMaterials) {
                if (material != null) {
                    material.SetFloat(Transition, percent);
                }
            }
        }
        
        void StartKandraChanges() {
            if (ParentModel.BodyFeatures() is { } bodyFeatures) {
                bodyFeatures.SetHairsAddBlock(true);
                bodyFeatures.RemoveAllHairFeatures();
            }
        }

        void StartKandraRevertChanges() {
            ParentModel.BodyFeatures()?.SetHairsAddBlock(false);
            if (_ctsList != null) {
                foreach (var cts in _ctsList) {
                    cts.Cancel();
                }
                _ctsList = null;
            }
        }

        void FinishKandraRevertChanges() {
            UpdateKandraMaterials(0);
        }
    }
}