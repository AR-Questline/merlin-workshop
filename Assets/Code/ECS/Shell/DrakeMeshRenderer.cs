using System;
using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    [ExecuteAlways, SelectionBase]
    public sealed class DrakeMeshRenderer : MonoBehaviour, IRenderingOptimizationSystemTarget, IDrakeStaticBakeable,
        IWithOcclusionCullingTarget, IPreviewDataProvider, IEcsRenderingProxy {
        public float4x4 LocalToWorld;
        public ref readonly float4x4 LocalToWorldOffset => throw new NotImplementedException();
        public DrakeRendererArchetypeKey[] ArchetypeKeys = Array.Empty<DrakeRendererArchetypeKey>();
        public RenderMeshDescription RenderMeshDescription(bool asStatic) => throw new NotImplementedException();
        public AssetReference MeshReference;
        public AssetReference[] MaterialReferences = Array.Empty<AssetReference>();
        public DrakeRendererVisibleRangeComponent VisibleRange;
        public ref readonly AABB AABB => throw new NotImplementedException();
        public int LodMask;
        public DrakeLodGroup Parent => throw new NotImplementedException();
        public bool IsBaked;
        public bool HasEntitiesAccess;
        public bool HasLinkedLifetime;
        public ref readonly AABB ExpandedBakingAABB => throw new NotImplementedException();
        public ref Material[] RuntimeOverrideMaterials => throw new NotImplementedException();

        public void Spawn() {
            throw new NotImplementedException();
        }

        public void Setup(MeshRenderer meshRenderer, MeshFilter meshFilter, DrakeLodGroup parentGroup,
            int lodMask, float4x4 localToWorldOffset,
            AssetReference meshReference, AssetReference[] materialReferences) {
            throw new NotImplementedException();
        }

        public void PrepareRanges(float4 lodDistances0, float4 lodDistances1) {
            throw new NotImplementedException();
        }

        public static float2 PrepareRanges(in float4 lodDistances0, in float4 lodDistances1, int lodMask) {
            throw new NotImplementedException();
        }

        public bool IsStatic => throw new NotImplementedException();

        public void BakeStatic() {
            throw new NotImplementedException();
        }

        public void SetUnityRepresentation(in IWithUnityRepresentation.Options options) {
            throw new NotImplementedException();
        }

        public void ChangeLayer(int? layer, uint? renderingLayerMask) {
            throw new NotImplementedException();
        }

        public void Clear(bool transformNeeded) {
            throw new NotImplementedException();
        }

        public void ClearData() {
            throw new NotImplementedException();
        }

        public void ResetModifiedBakingAABB() {
            throw new NotImplementedException();
        }

        public void EnsureBakingAABBExtents(float3 biggerExtents) {
            throw new NotImplementedException();
        }

        public void StartLoadingMaterials() {
            throw new NotImplementedException();
        }

        public void StartLoadingMaterial(int index) {
            throw new NotImplementedException();
        }

        public Material[] WaitForCompletionMaterials() {
            throw new NotImplementedException();
        }

        public void WaitForCompletionMaterials(ref List<Material> materials) {
            throw new NotImplementedException();
        }

        public Material WaitForCompletionMaterial(int index) {
            throw new NotImplementedException();
        }

        public void StartLoadingMesh() {
            throw new NotImplementedException();
        }

        public Mesh WaitForCompletionMesh() {
            throw new NotImplementedException();
        }

        public ref SerializableRenderMeshDescription SerializableRenderMeshDescription =>
            throw new NotImplementedException();

        public MinMaxAABB WorldBounds => throw new NotImplementedException();
        public (string, string) MeshReferenceData => throw new NotImplementedException();
        public int MaterialsCountWithOverrideCheck => throw new NotImplementedException();

        public (string, string) MaterialReferenceData(int i) => throw new NotImplementedException();
        public static Action<DrakeMeshRenderer> OnAddedDrakeMeshRenderer;
        public static Action<DrakeMeshRenderer> OnRemovedDrakeMeshRenderer;

        public static Func<DrakeMeshRenderer, IWithOcclusionCullingTarget.IRevertOcclusion>
            OnEnterOcclusionCullingCreator;

        public void EDITOR_SetMeshReference(AssetReference assetReference, Mesh mesh) {
            throw new NotImplementedException();
        }

        public void EDITOR_SetMaterialsReferences(AssetReference[] assetReferences, Material[] materials) {
            throw new NotImplementedException();
        }

        public IWithOcclusionCullingTarget.IRevertOcclusion EnterOcclusionCulling() {
            throw new NotImplementedException();
        }

        public void EDITOR_AssignParent(DrakeLodGroup group) {
            throw new NotImplementedException();
        }

        public DrawMeshDatum EDITOR_GetDrawMeshDatum() {
            throw new NotImplementedException();
        }

        public Material EDITOR_GetMaterial(int index) {
            throw new NotImplementedException();
        }

        public Material[] EDITOR_GetMaterials() {
            throw new NotImplementedException();
        }

        public void EDITOR_GetMaterials(ref List<Material> materials) {
            throw new NotImplementedException();
        }

        public Mesh EDITOR_GetMesh() {
            throw new NotImplementedException();
        }
    }
}