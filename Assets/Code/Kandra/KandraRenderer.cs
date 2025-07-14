using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Previews;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra {
    [ExecuteInEditMode]
    public class KandraRenderer : MonoBehaviour, IARPreviewProvider {
        public RendererData rendererData;

#if UNITY_EDITOR
        [field: NonSerialized] public bool EDITOR_Force_Uninitialized { get; set; }
#endif
        [field: NonSerialized]
        public uint RenderingId { get; set; } = KandraRendererManager.InvalidBitmask | KandraRendererManager.ValidBitmask;
        [field: NonSerialized] public bool Destroyed { get; set; }

        public ushort BlendshapesCount => (ushort)rendererData.blendshapeWeights.Length;

        void Awake() {
            EnsureInitialized();
            KandraRendererManager.Instance.StopTracking(this);
        }

        void OnEnable() {
#if UNITY_EDITOR
            if (rendererData.rig == null) {
                Log.Critical?.Error($"KandraRenderer {this} has no rig assigned", this);
                return;
            }
            // In editor lifetime is so messed up that it is possible
            EnsureInitialized();
            KandraRendererManager.Instance.StopTracking(this);
#endif
            KandraRendererManager.Instance.Register(this);
            rendererData.rig.RegisterActiveRenderer(this);
        }

        void OnDisable() {
            rendererData.rig.UnregisterActiveRenderer(this);

#if UNITY_EDITOR
            // In editor lifetime is so messed up that it is possible
            if (EDITOR_Force_Uninitialized || KandraRendererManager.Instance == null) {
                return;
            }
#endif

            KandraRendererManager.Instance.Unregister(this);
        }

        void OnDestroy() {
            if (!KandraRendererManager.Instance.IsRegistered(RenderingId)) {
                Dispose();
            } else {
                rendererData.rig.RemoveMerged(this);
            }

            Destroyed = true;
        }

        public void EnsureInitialized() {
#if UNITY_EDITOR
            try {
                EnsureInitializedImpl();
            } catch {
                var path = gameObject != null ? gameObject.PathInSceneHierarchy() : "GameObject is null!";
                Log.Critical?.Error($"Exception during Kandra initialization of {this}\n{path}", this);
                throw;
            }
#else
            EnsureInitializedImpl();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureInitializedImpl() {
            if (rendererData.materialsInstancesRefCount == null) {
                rendererData.originalMesh = KandraRenderingMesh.Invalid;
                rendererData.culledMesh = KandraRenderingMesh.Invalid;

                var materialBroker = KandraRendererManager.Instance.MaterialBroker;
                var materialsCount = rendererData.materials.Length;
                rendererData.materialsInstancesRefCount = new ushort[materialsCount];
                rendererData.materialsInstances = new Material[materialsCount];
                for (var i = 0; i < materialsCount; ++i) {
                    rendererData.materialsInstances[i] = materialBroker.GetMaterial(rendererData.materials[i], this);
                }
                var blendshapesCount = (ushort)rendererData.mesh.blendshapesNames.Length;
                rendererData.blendshapeWeights = blendshapesCount == 0 ? default : new UnsafeArray<float>(blendshapesCount, ARAlloc.Persistent);

                // TODO: Could/should/may be optimized to produce single "baked" shape for these constant ones
                if (rendererData.constantBlendshapes) {
                    var weights = rendererData.blendshapeWeights;
                    foreach (var constantBlendshape in rendererData.constantBlendshapes.blendshapes) {
                        if (weights.Length <= constantBlendshape.index) {
                            Log.Critical?.Error($"Constant blendshape index {constantBlendshape.index} is out of range", this);
                            continue;
                        }
                        weights[constantBlendshape.index] = constantBlendshape.value;
                    }
                }

                KandraRendererManager.Instance.StartTracking(this);
            }
        }

        public void Dispose() {
#if UNITY_EDITOR
            if (rendererData.culledMesh.IsValid) {
                Log.Critical?.Error($"There is culled mesh {this.rendererData.mesh} for {(this ? this.gameObject.PathInSceneHierarchy() : "<unknown>")} that was not released. State: {(this ? this.isActiveAndEnabled : "<unknown>")} was force unregistered {EDITOR_Force_Uninitialized}", this);
            }
            if (!KandraRendererManager.Instance.IsRegistered(RenderingId) && rendererData.originalMesh.IsValid) {
                Log.Critical?.Error($"There is original mesh {this.rendererData.mesh} for {(this ? this.gameObject.PathInSceneHierarchy() : "<unknown>")} that was not released. State: {(this ? this.isActiveAndEnabled : "<unknown>")} was force unregistered {EDITOR_Force_Uninitialized}", this);
            }
#endif

            rendererData.rig.RemoveMerged(this);

            rendererData.blendshapeWeights.DisposeIfCreated();
            var refCounts = rendererData.materialsInstancesRefCount;
            // In editor we can get OnDestroy without Awake
#if UNITY_EDITOR
            if (refCounts != null)
#endif
            {
                var materialBroker = KandraRendererManager.Instance.MaterialBroker;
                for (var i = 0; i < refCounts.Length; i++) {
                    if (refCounts[i] > 0) {
                        materialBroker.ReleaseInstancedMaterial(rendererData.materialsInstances[i]);
                    } else {
                        materialBroker.ReleaseMaterial(rendererData.materials[i], this);
                    }
                }
                rendererData.materialsInstancesRefCount = null;
                rendererData.materialsInstances = null;
            }
        }

        // Blendshapes
        public bool HasBlendshape(ushort blendshapeIndex) {
            return blendshapeIndex < rendererData.blendshapeWeights.Length;
        }

        public float GetBlendshapeWeight(ushort blendshapeIndex) {
#if UNITY_EDITOR
            if (blendshapeIndex >= rendererData.blendshapeWeights.Length) {
                Log.Critical?.Error($"Blendshape index {blendshapeIndex} is out of range", this);
            }
#endif
            return rendererData.blendshapeWeights[blendshapeIndex];
        }

        public string GetBlendshapeName(ushort blendshapeIndex) {
            return rendererData.mesh.blendshapesNames[blendshapeIndex];
        }

        public int GetBlendshapeIndex(string blendshapeName) {
            return Array.IndexOf(rendererData.mesh.blendshapesNames, blendshapeName);
        }

        public void SetBlendshapeWeight(ushort blendshapeIndex, float weight) {
#if UNITY_EDITOR
            if (blendshapeIndex >= rendererData.blendshapeWeights.Length) {
                Log.Critical?.Error($"Blendshape index {blendshapeIndex} is out of range", this);
            }
#endif
            rendererData.blendshapeWeights[blendshapeIndex] = weight;
        }

        public bool SetBlendshapeWeightChecked(ushort blendshapeIndex, float weight) {
            if (blendshapeIndex < rendererData.blendshapeWeights.Length) {
                rendererData.blendshapeWeights[blendshapeIndex] = weight;
                return true;
            }
            return false;
        }

        // Mesh
        public void EnsureMesh() {
            if (!rendererData.culledMesh.IsValid) {
                if (!rendererData.originalMesh.IsValid) {
                    rendererData.originalMesh = KandraRendererManager.Instance.MeshBroker.TakeOriginalMesh(rendererData.mesh);
                }
#if UNITY_EDITOR
                else {
                    Log.Important?.Error($"Original mesh is already taken for {this}", this);
                }
#endif
            }

#if UNITY_EDITOR
            if ((rendererData.culledMesh.IsValid | rendererData.originalMesh.IsValid) == false) {
                Log.Critical?.Error($"No mesh for {this}", this);
            }
#endif
        }

        public void UpdateCullableMesh(UnsafeArray<ushort>.Span indices, UnsafeArray<SubmeshData> submeshes) {
            if (rendererData.originalMesh.IsValid) {
                ReleaseOriginalMesh();
            }
            if (rendererData.culledMesh.IsValid) {
                KandraRendererManager.Instance.MeshBroker.ReleaseCullableMesh(rendererData.mesh, rendererData.culledMesh);
            }

            rendererData.culledMesh = KandraRendererManager.Instance.MeshBroker.CreateCullableMesh(rendererData.mesh, indices, submeshes);
            UpdateRenderingMesh();
        }

        public void ReleaseCullableMesh() {
            if (rendererData.culledMesh.IsValid) {
                KandraRendererManager.Instance.MeshBroker.ReleaseCullableMesh(rendererData.mesh, rendererData.culledMesh);
                rendererData.culledMesh = KandraRenderingMesh.Invalid;

                if (KandraRendererManager.Instance.IsRegistered(RenderingId)) {
                    rendererData.originalMesh = KandraRendererManager.Instance.MeshBroker.TakeOriginalMesh(rendererData.mesh);
                    UpdateRenderingMesh();
                }
            }
        }

        public void ReleaseOriginalMesh() {
            KandraRendererManager.Instance.MeshBroker.ReleaseOriginalMesh(rendererData.mesh);
            rendererData.originalMesh = KandraRenderingMesh.Invalid;
        }

        // Stitching
        public static void RedirectToRig(KandraRenderer source, KandraRenderer copy, KandraRig rig, ref UnsafeHashMap<FixedString64Bytes, ushort> bonesMap) {
            copy.rendererData = source.rendererData.Copy(copy.gameObject);
            rig.Merge(source.rendererData.rig, copy, copy.rendererData.bones, ref bonesMap, ref copy.rendererData.rootBone);
            copy.rendererData.rig = rig;
        }

        // Materials
        public Material[] GetOriginalMaterials() {
            return rendererData.materials;
        }
        
        public Material[] GetInstantiatedMaterials() {
            return rendererData.materialsInstances;
        }

        public Material[] UseInstancedMaterials() {
            var anyChanged = false;
            var refCounts = rendererData.materialsInstancesRefCount;

            var materialBroker = KandraRendererManager.Instance.MaterialBroker;

            List<Material> materialsToRelease = null;
            for (var i = 0; i < refCounts.Length; ++i) {
                if (refCounts[i] == 0) {
                    anyChanged = true;
                    rendererData.materialsInstances[i] = materialBroker.CreateInstanced(rendererData.materialsInstances[i], this);
                    materialsToRelease ??= new List<Material>(refCounts.Length-i);
                    materialsToRelease.Add(rendererData.materials[i]);
                }
                refCounts[i] = (ushort)(refCounts[i] + 1);
            }

            if (anyChanged) {
                UpdateRenderingMaterials();

                for (var i = 0; i < materialsToRelease.Count; ++i) {
                    materialBroker.ReleaseMaterial(materialsToRelease[i], this);
                }
                materialsToRelease.Clear();
            }

            return rendererData.RenderingMaterials;
        }

        public Material UseInstancedMaterial(int materialIndex) {
            var materialBroker = KandraRendererManager.Instance.MaterialBroker;

            var refCounts = rendererData.materialsInstancesRefCount;
            if (refCounts[materialIndex] == 0) {
                rendererData.materialsInstances[materialIndex] = materialBroker.CreateInstanced(rendererData.materialsInstances[materialIndex], this);
                UpdateRenderingMaterials();
                materialBroker.ReleaseMaterial(rendererData.materials[materialIndex], this);
            }
            refCounts[materialIndex] = (ushort)(refCounts[materialIndex] + 1);
            return rendererData.RenderingMaterials[materialIndex];
        }

        public void UseOriginalMaterials() {
            // After destroy, do nothing
            if (Destroyed) {
                return;
            }

            var materialBroker = KandraRendererManager.Instance.MaterialBroker;

            var anyChanged = false;
            var refCounts = rendererData.materialsInstancesRefCount;

            if (refCounts == null) {
                Log.Critical?.Error($"Cannot use original materials as Kandra is uninitialized {this}", this);
                return;
            }

            List<Material> materialsToDestroy = null;
            for (var i = 0; i < refCounts.Length; ++i) {
                if (refCounts[i] == 0) {
                    Log.Critical?.Error($"Material {rendererData.materials[i].name} is already original {this} {gameObject.PathInSceneHierarchy()}", this);
                    continue;
                }

                refCounts[i] = (ushort)(refCounts[i] - 1);
                if (refCounts[i] == 0) {
                    anyChanged = true;
                    materialsToDestroy ??= new List<Material>(refCounts.Length);
                    materialsToDestroy.Add(rendererData.materialsInstances[i]);
                    rendererData.materialsInstances[i] = materialBroker.GetMaterial(rendererData.materials[i], this);
                }
            }

            if (anyChanged) {
                UpdateRenderingMaterials();

                for (var i = 0; i < materialsToDestroy.Count; ++i) {
                    materialBroker.ReleaseInstancedMaterial(materialsToDestroy[i]);
                }
                materialsToDestroy.Clear();
            }
        }

        public void UseOriginalMaterial(int materialIndex) {
            // After destroy, do nothing
            if (Destroyed) {
                return;
            }

            var refCounts = rendererData.materialsInstancesRefCount;
            if (refCounts[materialIndex] == 0) {
                Log.Critical?.Error($"Material {rendererData.materials[materialIndex].name} is already original {this}", this);
                return;
            }

            refCounts[materialIndex] = (ushort)(refCounts[materialIndex] - 1);
            if (refCounts[materialIndex] == 0) {
                var materialsInstances = rendererData.materialsInstances;
                var materialInstance = materialsInstances[materialIndex];
                rendererData.materialsInstances[materialIndex] = KandraRendererManager.Instance.MaterialBroker.GetMaterial(rendererData.materials[materialIndex], this);
                UpdateRenderingMaterials();
                KandraRendererManager.Instance.MaterialBroker.ReleaseInstancedMaterial(materialInstance);
            }
        }
        
        public void UseOriginalMaterial(int materialIndex, Material newOriginalMaterial) {
            // After destroy, do nothing
            if (Destroyed) {
                return;
            }
            
            var materialBroker = KandraRendererManager.Instance.MaterialBroker;

            var refCounts = rendererData.materialsInstancesRefCount;
            Material materialToDestroy = null;
            Material materialToRelease = null;
            
            if (refCounts[materialIndex] == 0) {
                materialToRelease = rendererData.materials[materialIndex];
            } else if (refCounts[materialIndex] > 1) {
                Log.Critical?.Error($"Material {rendererData.materialsInstances[materialIndex].name} is instanced but will be changed to new original {this}", this);
                materialToDestroy = rendererData.materialsInstances[materialIndex];
            } else {
                materialToDestroy = rendererData.materialsInstances[materialIndex];
            }

            refCounts[materialIndex] = 0;
            rendererData.materials[materialIndex] = newOriginalMaterial;
            rendererData.materialsInstances[materialIndex] = materialBroker.GetMaterial(rendererData.materials[materialIndex], this);

            UpdateRenderingMaterials();

            if (materialToDestroy) {
                materialBroker.ReleaseInstancedMaterial(materialToDestroy);
            }

            if (materialToRelease) {
                materialBroker.ReleaseMaterial(materialToRelease, this);
            }
        }

        public void MaterialsTransparencyChanged() {
            UpdateRenderingMaterials();
        }

        public void SetFilteringSettings(in RendererFilteringSettings renderingData) {
            rendererData.filteringSettings = renderingData;
            RefreshFilterSettings();
        }

        public void RefreshFilterSettings() {
            if (KandraRendererManager.IsInvalidId(RenderingId) || KandraRendererManager.IsWaitingId(RenderingId)) {
                return;
            }
            KandraRendererManager.Instance.UpdateFilterSettings(RenderingId);
        }

        public void ChangeOriginalMaterials(Material[] newOriginalMaterials) {
            // After destroy, do nothing
            if (Destroyed) {
                return;
            }

            var materialBroker = KandraRendererManager.Instance.MaterialBroker;

            var refCounts = rendererData.materialsInstancesRefCount;
            List<Material> materialsToDestroy = null;
            var materialsToRelease = new List<Material>(refCounts.Length);
            for (var i = 0; i < refCounts.Length; ++i) {
                if (refCounts[i] == 0) {
                    materialsToRelease.Add(rendererData.materials[i]);
                } else {
                    Log.Critical?.Error($"Material {rendererData.materialsInstances[i].name} is instanced but will be changed to new original {this}", this);
                    materialsToDestroy ??= new List<Material>(refCounts.Length);
                    materialsToDestroy.Add(rendererData.materialsInstances[i]);
                }

                refCounts[i] = 0;
                rendererData.materials[i] = newOriginalMaterials[i];
                rendererData.materialsInstances[i] = materialBroker.GetMaterial(rendererData.materials[i], this);
            }

            UpdateRenderingMaterials();

            if (materialsToDestroy != null) {
                for (var i = 0; i < materialsToDestroy.Count; ++i) {
                    materialBroker.ReleaseInstancedMaterial(materialsToDestroy[i]);
                }
                materialsToDestroy.Clear();
            }

            for (var i = 0; i < materialsToRelease.Count; ++i) {
                materialBroker.ReleaseMaterial(materialsToRelease[i], this);
            }
            materialsToRelease.Clear();

            TexturesChanged();
        }

        public void TexturesChanged() {
            if (KandraRendererManager.IsInvalidId(RenderingId) || KandraRendererManager.IsWaitingId(RenderingId)) {
                return;
            }
            KandraRendererManager.Instance.UpdateMipmapsStreaming(RenderingId);
        }

        void UpdateRenderingMesh() {
            if (KandraRendererManager.IsInvalidId(RenderingId) || KandraRendererManager.IsWaitingId(RenderingId)) {
                return;
            }
            KandraRendererManager.Instance.UpdateSubmeshIndices(RenderingId, rendererData.RenderingMesh);
        }

        public void UpdateRenderingMaterials() {
            if (KandraRendererManager.IsInvalidId(RenderingId) || KandraRendererManager.IsWaitingId(RenderingId)) {
                return;
            }
            KandraRendererManager.Instance.UpdateRenderingMaterials(RenderingId, rendererData.RenderingMaterials, rendererData.RenderingMesh);
        }

        // Others
        public (ulong ownSize, ulong sharedSize) CollectMemorySize() {
            var ownSize = 0ul;
            var sharedSize = 0ul;

            sharedSize += KandraRendererManager.Instance.RigManager.GetMemoryUsageFor(rendererData.rig);
            sharedSize += KandraRendererManager.Instance.MeshManager.GetMemoryUsageFor(rendererData.mesh);
            sharedSize += KandraRendererManager.Instance.BlendshapesManager.GetMemoryUsageFor(rendererData.mesh);
            if (rendererData.originalMesh.IsValid) {
                var submeshes = rendererData.originalMesh.submeshes;
                for (var i = 0u; i < submeshes.Length; ++i) {
                    sharedSize += submeshes[i].indexCount;
                }
            }

            ownSize += KandraRendererManager.Instance.BonesManager.GetMemoryUsageFor(RenderingId);
            ownSize += KandraRendererManager.Instance.SkinningManager.GetMemoryUsageFor(RenderingId);
            if (rendererData.culledMesh.IsValid) {
                var submeshes = rendererData.culledMesh.submeshes;
                for (var i = 0u; i < submeshes.Length; ++i) {
                    sharedSize += submeshes[i].indexCount;
                }
            }

            return (ownSize, sharedSize);
        }

        public void DrawMemoryInfo() {
            if (KandraRendererManager.Instance.RigManager.TryGetMemoryRegionFor(rendererData.rig, out var rigMemory)) {
                var memory = KandraRendererManager.Instance.RigManager.GetMemoryUsageFor(rendererData.rig);
                GUILayout.Label($"Rig: {rigMemory} {M.HumanReadableBytes(memory)}");
            } else {
                GUILayout.Label("No rig region");
            }

            if (KandraRendererManager.Instance.MeshManager.TryGetMeshMemory(rendererData.mesh, out var meshMemory)) {
                var memory = KandraRendererManager.Instance.MeshManager.GetMemoryUsageFor(rendererData.mesh);
                GUILayout.Label($"Mesh: {meshMemory} {M.HumanReadableBytes(memory)}");
            } else {
                GUILayout.Label("No mesh region");
            }

            if (KandraRendererManager.Instance.BonesManager.TryGetBonesMemory(RenderingId, out var bonesMemory)) {
                var memory = KandraRendererManager.Instance.BonesManager.GetMemoryUsageFor(RenderingId);
                GUILayout.Label($"Bones: {bonesMemory} {M.HumanReadableBytes(memory)}");
            } else {
                GUILayout.Label("No bones region");
            }

            if (KandraRendererManager.Instance.SkinningManager.TryGetSkinnedVerticesMemory(RenderingId, out var skinnedVertsMemory)) {
                var memory = KandraRendererManager.Instance.SkinningManager.GetMemoryUsageFor(RenderingId);
                GUILayout.Label($"Skinned verts: {skinnedVertsMemory} {M.HumanReadableBytes(memory)}");
            } else {
                GUILayout.Label("No skinned verts region");
            }

            if (KandraRendererManager.Instance.BlendshapesManager.TryGetBlendshapesData(rendererData.mesh, out var blendshapesMemory)) {
                string joined = string.Empty;
                if (blendshapesMemory.Length > 2) {
                    joined = $"{blendshapesMemory[0]},.{blendshapesMemory.Length-2}.,{blendshapesMemory[blendshapesMemory.Length-1]}";
                } else {
                    joined = string.Join(", ", blendshapesMemory.AsNativeArray());
                }
                var memory = KandraRendererManager.Instance.BlendshapesManager.GetMemoryUsageFor(rendererData.mesh);
                GUILayout.Label($"Blends: {joined} {M.HumanReadableBytes(memory)}");
            } else {
                GUILayout.Label("No blends region");
            }
        }

        // Editor
#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            if (KandraRendererManager.IsInvalidId(RenderingId) || KandraRendererManager.IsWaitingId(RenderingId)) {
                return;
            }

            KandraRendererManager.Instance.GetBoundsAndRootBone(RenderingId, out var worldBoundingSphere, out var rootBoneMatrix);
            var bounds = rendererData.mesh.meshLocalBounds;
            var oldMatrix = Gizmos.matrix;
            Gizmos.color = KandraRendererManager.Instance.IsCameraVisible(RenderingId) ? Color.green : Color.red;
            Gizmos.DrawWireSphere(worldBoundingSphere.xyz, worldBoundingSphere.w);
            Gizmos.matrix = rootBoneMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.matrix = oldMatrix;
        }

        public void EDITOR_RenderingDataChanged() {
            EDITOR_ClearMaterials();
            EDITOR_RecreateMaterials();
        }

        public void EDITOR_ClearMaterials() {
            var materialBroker = KandraRendererManager.Instance.MaterialBroker;
            var oldMaterialsCount = rendererData.materialsInstancesRefCount.Length;
            for (var i = 0; i < oldMaterialsCount; ++i) {
                if (rendererData.materialsInstancesRefCount[i] > 0) {
                    DestroyImmediate(rendererData.materialsInstances[i]);
                } else {
                    materialBroker.ReleaseMaterial(rendererData.materials[i], this);
                }
            }
            rendererData.materialsInstancesRefCount = Array.Empty<ushort>();
            rendererData.materialsInstances = Array.Empty<Material>();
        }

        public void EDITOR_RecreateMaterials() {
            var materialBroker = KandraRendererManager.Instance.MaterialBroker;
            rendererData.materialsInstancesRefCount = new ushort[rendererData.materials.Length];
            rendererData.materialsInstances = new Material[rendererData.materials.Length];
            for (var i = 0; i < rendererData.materials.Length; ++i) {
                rendererData.materialsInstances[i] = materialBroker.GetMaterial(rendererData.materials[i], this);
            }
            UpdateRenderingMaterials();
        }
#endif

        // Preview
        public static Func<KandraRenderer, IEnumerable<IARRendererPreview>> PreviewCreator { get; set; }
        public IEnumerable<IARRendererPreview> GetPreviews() {
            return PreviewCreator?.Invoke(this);
        }

        [Serializable]
        public struct RendererData {
            // -- Authoring
            public KandraRig rig;

            public KandraMesh mesh;
            public KandraBoundsAmplifier boundsAmplifier;

#if UNITY_EDITOR
#if ADDRESSABLES_BUILD
            [NonSerialized]
#endif
            public Mesh EDITOR_sourceMesh;

            [NonSerialized] Material[] _editorMaterials;
#endif

            public Material[] materials;
            [NonSerialized] public ushort[] materialsInstancesRefCount;
            [NonSerialized] public Material[] materialsInstances;

            public ushort[] bones;
            public ushort rootBone;
            public float3x4 rootBoneMatrix;

            public RendererFilteringSettings filteringSettings;

            public ConstantKandraBlendshapes constantBlendshapes;

            // -- Runtime
            [NonSerialized] public KandraRenderingMesh originalMesh;
            [NonSerialized] public KandraRenderingMesh culledMesh;

            public UnsafeArray<float> blendshapeWeights;

            public KandraRenderingMesh RenderingMesh => culledMesh.IsValid ? culledMesh : originalMesh;
            public Material[] RenderingMaterials => materialsInstances;
            public int MaterialsCount => materials.Length;

            public RendererData Copy(GameObject target) {
                ConstantKandraBlendshapes resultConstantBlendshapes = null;
                if (constantBlendshapes) {
                    resultConstantBlendshapes = target.AddComponent<ConstantKandraBlendshapes>();
                    resultConstantBlendshapes.blendshapes = constantBlendshapes.blendshapes.CreateCopy();
                }

                return new RendererData {
                    rig = rig,
                    mesh = mesh,
                    materials = materials.CreateCopy(),
                    bones = bones.CreateCopy(),
                    rootBone = rootBone,
                    rootBoneMatrix = rootBoneMatrix,
                    filteringSettings = filteringSettings,
                    constantBlendshapes = resultConstantBlendshapes
                };
            }
        }

        [Serializable]
        public struct RendererFilteringSettings : IEquatable<RendererFilteringSettings> {
            public ShadowCastingMode shadowCastingMode;
            public uint renderingLayersMask;

            public RendererFilteringSettings(ShadowCastingMode shadowCastingMode, uint renderingLayersMask) {
                this.shadowCastingMode = shadowCastingMode;
                this.renderingLayersMask = renderingLayersMask;
            }

            public bool Equals(RendererFilteringSettings other) {
                return shadowCastingMode == other.shadowCastingMode && renderingLayersMask == other.renderingLayersMask;
            }
            public override bool Equals(object obj) {
                return obj is RendererFilteringSettings other && Equals(other);
            }
            public override int GetHashCode() {
                unchecked {
                    return ((int)shadowCastingMode * 397) ^ (int)renderingLayersMask;
                }
            }
            public static bool operator ==(RendererFilteringSettings left, RendererFilteringSettings right) {
                return left.Equals(right);
            }
            public static bool operator !=(RendererFilteringSettings left, RendererFilteringSettings right) {
                return !left.Equals(right);
            }
        }
    }
}
