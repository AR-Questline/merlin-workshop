using System;
using System.Collections.Generic;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Previews;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra {
    public class KandraRenderer : MonoBehaviour, IARPreviewProvider {
        public static Func<KandraRenderer, IEnumerable<IARRendererPreview>> PreviewCreator { get; set; }
        public RendererData rendererData;
        public bool EDITOR_Force_Uninitialized;

        public IEnumerable<IARRendererPreview> GetPreviews() {
            throw new NotImplementedException();
        }

        public uint RenderingId { get; set; }
        public bool Destroyed { get; set; }
        public int BlendshapesCount { get; set; }

        public struct RendererData {
            public KandraRig rig;
            public KandraMesh mesh;
            public KandraBoundsAmplifier boundsAmplifier;


            public Mesh EDITOR_sourceMesh;
            public Material[] materials;
            public ushort[] materialsInstancesRefCount;
            public Material[] materialsInstances;

            public ushort[] bones;
            public ushort rootBone;
            public float3x4 rootBoneMatrix;
            public RendererFilteringSettings filteringSettings;
            public ConstantKandraBlendshapes constantBlendshapes;

            public KandraRenderingMesh originalMesh;
            public KandraRenderingMesh culledMesh;
            public UnsafeArray<float> blendshapeWeights;
            public KandraRenderingMesh RenderingMesh;
            public Material[] RenderingMaterials;
            public int MaterialsCount;

            public RendererData Copy(GameObject target) {
                throw new NotImplementedException();
            }
        }

        public struct RendererFilteringSettings : IEquatable<RendererFilteringSettings> {
            public ShadowCastingMode shadowCastingMode;
            public uint renderingLayersMask;

            public RendererFilteringSettings(ShadowCastingMode shadowCastingMode, uint renderingLayersMask) {
                throw new NotImplementedException();
            }

            public bool Equals(RendererFilteringSettings other) {
                throw new NotImplementedException();
            }

            public override bool Equals(object obj) {
                throw new NotImplementedException();
            }

            public override int GetHashCode() {
                throw new NotImplementedException();
            }

            public static bool operator ==(RendererFilteringSettings left, RendererFilteringSettings right) {
                throw new NotImplementedException();
            }

            public static bool operator !=(RendererFilteringSettings left, RendererFilteringSettings right) {
                throw new NotImplementedException();
            }
        }

        public void EnsureInitialized() {
            throw new NotImplementedException();
        }

        public void ReleaseCullableMesh() {
            throw new NotImplementedException();
        }

        public void UpdateCullableMesh(UnsafeArray<ushort> culledIndices, UnsafeArray<SubmeshData> newSubmeshes) {
            throw new NotImplementedException();
        }

        public void EnsureMesh() {
            throw new NotImplementedException();
        }

        public void ReleaseOriginalMesh() {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public Material[] UseInstancedMaterials() {
            throw new NotImplementedException();
        }

        public void UseOriginalMaterials() {
            throw new NotImplementedException();
        }

        public bool HasBlendshape(ushort blendshapeRedirectKandraIndex) {
            throw new NotImplementedException();
        }

        public void SetBlendshapeWeight(ushort blendshapeRedirectKandraIndex, float getBlendShapeWeight) {
            throw new NotImplementedException();
        }

        public (ulong ownSize, ulong sharedSize) CollectMemorySize() {
            throw new NotImplementedException();
        }

        public void DrawMemoryInfo() {
            throw new NotImplementedException();
        }

        public Material UseInstancedMaterial(int eyesIndex) {
            throw new NotImplementedException();
        }

        public void UseOriginalMaterial(int eyesIndex) {
            throw new NotImplementedException();
        }

        public Material[] GetOriginalMaterials() {
            throw new NotImplementedException();
        }

        public void ChangeOriginalMaterials(Material[] materials) {
            throw new NotImplementedException();
        }

        public IEnumerable<Material> GetInstantiatedMaterials() {
            throw new NotImplementedException();
        }

        public void SetFilteringSettings(RendererFilteringSettings newFilterSettings) {
            throw new NotImplementedException();
        }

        public static void RedirectToRig(KandraRenderer sourceCloth, KandraRenderer realCloth, KandraRig baseRig,
            ref UnsafeHashMap<FixedString64Bytes, ushort> bonesMap) {
            throw new NotImplementedException();
        }

        public string GetBlendshapeName(ushort @ushort) {
            throw new NotImplementedException();
        }

        public int GetBlendshapeIndex(string blendshapeName) {
            throw new NotImplementedException();
        }

        public float GetBlendshapeWeight(ushort mappingY) {
            throw new NotImplementedException();
        }

        public bool SetBlendshapeWeightChecked(ushort mappingX, object sourceWeight) {
            throw new NotImplementedException();
        }

        public void TexturesChanged() {
            throw new NotImplementedException();
        }

        public void MaterialsTransparencyChanged() {
            throw new NotImplementedException();
        }

        public void RefreshFilterSettings() {
            throw new NotImplementedException();
        }

        public void UseOriginalMaterial(int materialIndex, Material newOriginalMaterial) {
            throw new NotImplementedException();
        }

        public void EDITOR_ClearMaterials() {
            throw new NotImplementedException();
        }

        public void EDITOR_RecreateMaterials() {
            throw new NotImplementedException();
        }

        public void EDITOR_RenderingDataChanged() {
            throw new NotImplementedException();
        }

        public void UpdateRenderingMaterials() {
            throw new NotImplementedException();
        }
    }
}