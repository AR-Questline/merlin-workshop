using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace Awaken.Kandra.Managers {
    public class MaterialBroker {
        const string KandraSkinningKeyword = "KANDRA_SKINNING";
        
        Dictionary<int, MaterialData> _originalMaterials = new(KandraRendererManager.FinalRenderersCapacity);

        public Material GetMaterial(Material material, KandraRenderer debugTarget) {
            if (material == null) {
                Log.Critical?.Error($"Getting null material for {debugTarget.gameObject.HierarchyPath()}", debugTarget);
                return null;
            }
            var materialId = material.GetHashCode();
            if (_originalMaterials.TryGetValue(materialId, out var materialData)) {
                if (materialData.material == null) { // TODO: why it was destroyed?
                    Log.Critical?.Error($"Kandra material from {material} was destroyed", debugTarget);
                    _originalMaterials.Remove(materialId);
                } else {
                    ++materialData.referenceCount;
                    _originalMaterials[materialId] = materialData;
                    return materialData.material;
                }
            }
            var newMaterial = CreateKandraEnabledMaterial(material, debugTarget);
            _originalMaterials.Add(materialId, new MaterialData { material = newMaterial, referenceCount = 1 });
            return newMaterial;
        }

        public Material CreateInstanced(Material kandraMaterial, KandraRenderer debugTarget) {
            return Object.Instantiate(kandraMaterial);
        }

        public void ReleaseMaterial(Material material, KandraRenderer debugTarget) {
            if (material == null) {
                Log.Critical?.Error($"Releasing null material for {debugTarget}", debugTarget);
                return;
            }
            var materialId = material.GetHashCode();
            if (!_originalMaterials.TryGetValue(materialId, out var materialData)) {
                return;
            }

            --materialData.referenceCount;
            if (materialData.referenceCount == 0) {
#if UNITY_EDITOR
                if (Application.isPlaying) {
                    UnityEngine.Object.Destroy(materialData.material);
                } else {
                    UnityEngine.Object.DestroyImmediate(materialData.material);
                }
#else
                UnityEngine.Object.Destroy(materialData.material);
#endif
                _originalMaterials.Remove(materialId);
            } else {
                _originalMaterials[materialId] = materialData;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseInstancedMaterial(Material material) {
#if UNITY_EDITOR
            if (Application.isPlaying) {
                UnityEngine.Object.Destroy(material);
            } else {
                UnityEngine.Object.DestroyImmediate(material);
            }
#else
            UnityEngine.Object.Destroy(material);
#endif
        }

        public void Editor_OnMaterialChanged(Material material) {
            var key = material.GetHashCode();
            if (_originalMaterials.TryGetValue(key, out var materialData)) {
                // Copy material properties
                materialData.material.CopyPropertiesFromMaterial(material);
                ApplyKandraKeyword(material, null, materialData.material);
                HDMaterial.ValidateMaterial(materialData.material);
            }
        }

        Material CreateKandraEnabledMaterial(Material material, KandraRenderer debugTarget) {
            var newMaterial = new Material(material);
            newMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;
            ApplyKandraKeyword(material, debugTarget, newMaterial);
            return newMaterial;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ApplyKandraKeyword(Material material, KandraRenderer debugTarget, Material newMaterial) {
            // TODO: Check if this should be cached
            if (Array.IndexOf(newMaterial.shader.keywordSpace.keywordNames, KandraSkinningKeyword) >= 0) {
                var keywords = new LocalKeyword(newMaterial.shader, KandraSkinningKeyword);
                newMaterial.SetKeyword(keywords, true);
            } else {
                var shader = newMaterial.shader;
                Log.Critical?.Error($"Shader {shader.name} [material {material.name}] does not have {KandraSkinningKeyword} keyword for renderer {debugTarget}",
                    debugTarget ?? (UnityEngine.Object)material);
            }
        }

        struct MaterialData {
            public Material material;
            public ushort referenceCount;
        }
    }
}