using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Graphics;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    [ExecuteAlways, SelectionBase]
    public sealed class DrakeMeshRenderer : MonoBehaviour, IRenderingOptimizationSystemTarget, IDrakeStaticBakeable,
        IWithOcclusionCullingTarget, IPreviewDataProvider, IEcsRenderingProxy {
        const float LastLodDistanceMultiplier = 2f;
        const float DistancePreloadWidth = 0.5f;
        const float ApproximateDistanceScale = 1.6f;
        const float DistanceMinDifference = ApproximateDistanceScale * 10;

        [SerializeField] DrakeLodGroup parentGroup;
        [SerializeField] AssetReference meshReference;
        [SerializeField] AssetReference[] materialReferences = Array.Empty<AssetReference>();
        [SerializeField] SerializableRenderMeshDescription renderMeshDescription;
        [SerializeField] float4x4 localToWorldOffset = float4x4.identity;
        [SerializeField] AABB aabb;
        [SerializeField] float2 visibleRange;
        [SerializeField] int lodMask;
        [SerializeField] DrakeRendererArchetypeKey[] _archetypeKeys = Array.Empty<DrakeRendererArchetypeKey>();

#if ADDRESSABLES_BUILD
        [SerializeField]
#endif
        bool _hasEntitiesAccess;
#if ADDRESSABLES_BUILD
        [SerializeField]
#endif
        bool _hasLinkedLifetime;

        AABB _expandedBakingAABB;
        Material[] _runtimeOverrideMaterials;

        public float4x4 LocalToWorld => math.mul(transform.localToWorldMatrix, localToWorldOffset);
        public ref readonly float4x4 LocalToWorldOffset => ref localToWorldOffset;
        public DrakeRendererArchetypeKey[] ArchetypeKeys => _archetypeKeys;
        public RenderMeshDescription RenderMeshDescription(bool asStatic) => PrepareRuntimeMeshDescriptor(asStatic);
        public AssetReference MeshReference => meshReference;
        public AssetReference[] MaterialReferences => materialReferences;
        public DrakeRendererVisibleRangeComponent VisibleRange => new(visibleRange);
        public ref readonly AABB AABB => ref aabb;
        public int LodMask => lodMask;

        public DrakeLodGroup Parent => parentGroup;

        public bool IsBaked => materialReferences.Length > 0;
        public bool IsStatic => _archetypeKeys.Length > 0 && _archetypeKeys[0].isStatic;
        public bool HasEntitiesAccess => _hasEntitiesAccess | _hasLinkedLifetime;
        public bool HasLinkedLifetime => _hasLinkedLifetime;

        public ref readonly AABB ExpandedBakingAABB => ref _expandedBakingAABB;
        public ref Material[] RuntimeOverrideMaterials => ref _runtimeOverrideMaterials;

        void Start() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return;
            }
#endif
            Spawn();
        }

        public void Spawn() {
            if (parentGroup) {
                return;
            }
#if UNITY_EDITOR
            if (Application.isPlaying) {
#endif
                var myGameObject = gameObject;
                DrakeRendererManager.Instance.Register(this, myGameObject.scene);
#if UNITY_EDITOR
            } else {
                DrakeRendererManager.Instance.Register(this, gameObject.scene, default, default, false);
            }
#endif
        }

        public void Setup(MeshRenderer meshRenderer, MeshFilter meshFilter, DrakeLodGroup parentGroup,
            int lodMask, float4x4 localToWorldOffset,
            AssetReference meshReference, AssetReference[] materialReferences) {
            this.renderMeshDescription = new SerializableRenderMeshDescription(meshRenderer);
            this.parentGroup = parentGroup;
            this.localToWorldOffset = localToWorldOffset;
            this.visibleRange = new float2(0, float.PositiveInfinity);
            this.meshReference = meshReference;
            this.materialReferences = materialReferences;
            this.lodMask = lodMask;
            aabb = meshFilter.sharedMesh.bounds.ToAABB();

            var isStatic = gameObject.isStatic;
            var lightProbes = meshRenderer.lightProbeUsage;
            var hasLod = parentGroup != null;
            _archetypeKeys = new DrakeRendererArchetypeKey[materialReferences.Length];
            var materials = meshRenderer.sharedMaterials;
            for (var i = 0; i < materialReferences.Length; i++) {
                var isTransparent = ShadersUtils.IsMaterialTransparent(materials[i]);
                _archetypeKeys[i] = new DrakeRendererArchetypeKey(isStatic, isTransparent, hasLod,
                    renderMeshDescription.motionVectorGenerationMode != MotionVectorGenerationMode.Camera, lightProbes, false, false);
            }

            DestroyImmediate(meshRenderer);
            DestroyImmediate(meshFilter);
        }

        public void PrepareRanges(float4 lodDistances0, float4 lodDistances1) {
#if UNITY_EDITOR
            var startingIndex = math.tzcnt(lodMask);
            if (startingIndex >= 8) {
                Log.Important?.Error($"Invalid lod index {startingIndex}", this);
            }
#endif

            visibleRange = PrepareRanges(lodDistances0, lodDistances1, lodMask);
        }

        public static float2 PrepareRanges(in float4 lodDistances0, in float4 lodDistances1, int lodMask) {
            var startingIndex = math.tzcnt(lodMask);
            var startDistance = GetStartDistance(lodDistances0, lodDistances1, startingIndex);
            var endingIndex = math.min(31 - math.lzcnt(lodMask), 7);
            var endDistance = GetEndDistance(lodDistances0, lodDistances1, endingIndex);

            var expandedStart = math.max(startingIndex - 1, 0);
            var expandedStartDistance = GetStartDistance(lodDistances0, lodDistances1, expandedStart);
            var expandedEnd = math.min(endingIndex + 1, 7);
            var expandedEndDistance = GetEndDistance(lodDistances0, lodDistances1, expandedEnd);
            if (expandedEndDistance is float.PositiveInfinity or float.MaxValue) {
                expandedEndDistance = endDistance * LastLodDistanceMultiplier;
            }

            var startRange = math.lerp(startDistance, expandedStartDistance, DistancePreloadWidth);
            startRange = math.min(startRange, startDistance - DistanceMinDifference);
            startRange = math.max(startRange, 0);

            var endRange = math.lerp(endDistance, expandedEndDistance, DistancePreloadWidth);
            endRange = math.max(endRange, endDistance + DistanceMinDifference);

            return new float2(startRange * startRange, endRange * endRange);
        }

        public void BakeStatic() {
            var isStatic = parentGroup ? parentGroup.IsStatic : gameObject.isStatic;
            SetStatic(isStatic);
        }

        public void SetUnityRepresentation(in IWithUnityRepresentation.Options options) {
            if (options.linkedLifetime.HasValue) {
                _hasLinkedLifetime = options.linkedLifetime.Value;
            }

            if (options.movable.HasValue) {
                SetStatic(!options.movable.Value);
            }

            if (options.requiresEntitiesAccess.HasValue) {
                _hasEntitiesAccess = options.requiresEntitiesAccess.Value;
            }
        }

        public void ChangeLayer(int? layer, uint? renderingLayerMask) {
            if (layer.HasValue) {
                renderMeshDescription.OverrideLayer(layer.Value);
                gameObject.layer = layer.Value;
            }

            if (renderingLayerMask.HasValue) {
                renderMeshDescription.OverrideRenderingLayerMask(renderingLayerMask.Value);
            }
        }

        public void Clear(bool transformNeeded) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return;
            }
#endif
            if (!transformNeeded && transform.IsLeafSingleComponent()) {
                Destroy(gameObject);
            } else {
                Destroy(this);
            }
        }

        public void ClearData() {
            renderMeshDescription = default;
            localToWorldOffset = float4x4.identity;
            meshReference = default;
            materialReferences = Array.Empty<AssetReference>();
            aabb = default;
            visibleRange = default;
            parentGroup = default;
            lodMask = default;
            _archetypeKeys = Array.Empty<DrakeRendererArchetypeKey>();
        }

        public void ResetModifiedBakingAABB() {
            _expandedBakingAABB = aabb;
        }

        public void EnsureBakingAABBExtents(float3 biggerExtents) {
            _expandedBakingAABB.EnsureExtents(biggerExtents);
        }

        public void StartLoadingMaterials() {
            if (RuntimeOverrideMaterials is { Length: > 0 }) {
                return;
            }

            int materialsCount = materialReferences.Length;
            for (int i = 0; i < materialsCount; i++) {
                try {
                    var materialRef = materialReferences[i];
                    if (materialRef.IsValid() == false) {
                        materialRef.LoadAssetAsync<Material>();
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        public void StartLoadingMaterial(int index) {
            if (index < 0) {
                Log.Important?.Error($"Index {index} is invalid");
                return;
            }

            if (RuntimeOverrideMaterials is { Length: > 0 }) {
                if (index >= RuntimeOverrideMaterials.Length) {
                    Log.Important?.Error($"Trying to load material at index {index} but there are only {RuntimeOverrideMaterials.Length} {nameof(RuntimeOverrideMaterials)}");
                }

                return;
            }

            if (index >= materialReferences.Length) {
                Log.Important?.Error($"Trying to load material at index {index} but there are only {materialReferences.Length} {nameof(materialReferences)}");
                return;
            }

            try {
                var materialRef = materialReferences[index];
                if (materialRef.IsValid() == false) {
                    materialRef.LoadAssetAsync<Material>();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public Material[] WaitForCompletionMaterials() {
            int count = MaterialsCountWithOverrideCheck;
            var materials = new Material[count];
            var materialsIList = (IList<Material>)materials;
            WaitForCompletionMaterials(ref materialsIList, 0);
            return materials;
        }

        public void WaitForCompletionMaterials(ref List<Material> materials) {
            int count = MaterialsCountWithOverrideCheck;
            int startIndex = materials.Count;
            for (int i = 0; i < count; i++) {
                materials.Add(null);
            }

            var materialsIList = (IList<Material>)materials;
            WaitForCompletionMaterials(ref materialsIList, startIndex);
        }

        void WaitForCompletionMaterials(ref IList<Material> materials, int startIndex) {
            if (RuntimeOverrideMaterials is { Length: > 0 }) {
                for (int i = 0; i < RuntimeOverrideMaterials.Length; i++) {
                    materials[startIndex + i] = RuntimeOverrideMaterials[i];
                }

                return;
            }

            int materialsCount = materialReferences.Length;
            for (int i = 0; i < materialsCount; i++) {
                try {
                    var material = materialReferences[i].OperationHandle.WaitForCompletion() as Material;
                    if (material == null) {
                        Log.Important?.Error($"Failed to load Material from DrakeMeshRenderer {name} material ref at index {i} {materialReferences[i].RuntimeKey}", this);
                        materials[startIndex + i] = null;
                        continue;
                    }

                    materials[startIndex + i] = material;
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        public Material WaitForCompletionMaterial(int index) {
            if (index < 0) {
                Log.Important?.Error($"Index {index} is invalid");
                return null;
            }

            if (RuntimeOverrideMaterials is { Length: > 0 }) {
                if (index >= RuntimeOverrideMaterials.Length) {
                    Log.Important?.Error($"Trying to load material at index {index} but there are only {RuntimeOverrideMaterials.Length} {nameof(RuntimeOverrideMaterials)}");
                    return null;
                }

                return RuntimeOverrideMaterials[index];
            }

            if (index >= materialReferences.Length) {
                Log.Important?.Error($"Trying to load material at index {index} but there are only {materialReferences.Length} {nameof(materialReferences)}");
                return null;
            }

            try {
                var material = materialReferences[index].OperationHandle.WaitForCompletion() as Material;
                if (material == null) {
                    Log.Important?.Error($"Failed to load Material from DrakeMeshRenderer {name} material ref at index {index} {materialReferences[index].RuntimeKey}", this);
                }

                return material;
            } catch (Exception e) {
                Debug.LogException(e);
                return null;
            }
        }

        public void StartLoadingMesh() {
            if (meshReference == null) {
                Log.Important?.Error($"Mesh reference on {nameof(DrakeMeshRenderer)} {name} is null", this);
                return;
            }

            try {
                if (meshReference.IsValid() == false) {
                    meshReference.LoadAssetAsync<Mesh>();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public Mesh WaitForCompletionMesh() {
            if (meshReference == null) {
                Log.Important?.Error($"Mesh reference on {nameof(DrakeMeshRenderer)} {name} is null", this);
                return null;
            }

            try {
                return meshReference.OperationHandle.WaitForCompletion() as Mesh;
            } catch (Exception e) {
                Debug.LogException(e);
                return null;
            }
        }

        void SetStatic(bool isStatic) {
            for (int i = 0; i < _archetypeKeys.Length; i++) {
                _archetypeKeys[i].isStatic = isStatic;
            }
        }

        RenderMeshDescription PrepareRuntimeMeshDescriptor(bool asStatic) {
            var desc = renderMeshDescription.ToRenderMeshDescription(asStatic);
            var filterSettings = desc.FilterSettings;
            filterSettings.Layer = gameObject.layer;
            desc.FilterSettings = filterSettings;
            return desc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetStartDistance(float4 distance0, float4 distance1, int index) {
            if (index == 0) {
                return 0;
            }

            index = math.min(index, 7);

            --index;
            return index < 4 ? distance0[index] : distance1[index - 4];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetEndDistance(float4 distance0, float4 distance1, int index) {
            if (index < 4) {
                return distance0[index];
            }

            return distance1[index - 4];
        }

        // === HLOD support
        public ref SerializableRenderMeshDescription SerializableRenderMeshDescription => ref renderMeshDescription;
        public MinMaxAABB WorldBounds => AABB.Transform(LocalToWorld, aabb);
        public (string, string) MeshReferenceData => (meshReference.AssetGUID, meshReference.SubObjectName);
        public int MaterialsCountWithOverrideCheck => RuntimeOverrideMaterials is { Length: > 0 } ? RuntimeOverrideMaterials.Length : MaterialReferences.Length;

        public (string, string) MaterialReferenceData(int i) => (materialReferences[i].AssetGUID, materialReferences[i].SubObjectName);

        // === EDITOR ===
#if UNITY_EDITOR
        public static Action<DrakeMeshRenderer> OnAddedDrakeMeshRenderer;
        public static Action<DrakeMeshRenderer> OnRemovedDrakeMeshRenderer;
        public static Func<DrakeMeshRenderer, IWithOcclusionCullingTarget.IRevertOcclusion> OnEnterOcclusionCullingCreator;

        void OnEnable() {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            var scene = gameObject.scene;
            if (!scene.IsValid() || scene == default) {
                return;
            }

            if (IsBaked) {
                OnAddedDrakeMeshRenderer?.Invoke(this);
            }
        }

        void OnDisable() {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            Scene scene = gameObject.scene;
            if (!scene.IsValid() || scene == default) {
                return;
            }

            OnRemovedDrakeMeshRenderer?.Invoke(this);
        }

        public void EDITOR_SetMeshReference(AssetReference assetReference, Mesh mesh) {
            meshReference = assetReference;
            aabb = mesh.bounds.ToAABB();
        }

        public void EDITOR_SetMaterialsReferences(AssetReference[] assetReferences, Material[] materials) {
            Array.Resize(ref _archetypeKeys, materials.Length);
            var lastIndex = materialReferences.Length - 1;
            for (var i = materialReferences.Length; i < materials.Length; i++) {
                var isTransparent = ShadersUtils.IsMaterialTransparent(materials[i]);
                _archetypeKeys[i] = _archetypeKeys[lastIndex];
                _archetypeKeys[i].isTransparent = isTransparent;
            }

            materialReferences = assetReferences;
        }

        static T EDITOR_LoadAsset<T>(AssetReference assetReference) where T : UnityEngine.Object {
            if (string.IsNullOrEmpty(assetReference.SubObjectName)) {
                return assetReference.editorAsset as T;
            }

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID);
            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in allAssets) {
                if (asset is T castedAsset && castedAsset.name == assetReference.SubObjectName) {
                    return castedAsset;
                }
            }

            return assetReference.editorAsset as T;
        }
#endif

        public IWithOcclusionCullingTarget.IRevertOcclusion EnterOcclusionCulling() {
#if UNITY_EDITOR && !SIMULATE_BUILD
            return OnEnterOcclusionCullingCreator?.Invoke(this) ?? IWithOcclusionCullingTarget.TargetRevertDummy;
#else
            return IWithOcclusionCullingTarget.TargetRevertDummy;
#endif
        }

#if UNITY_EDITOR
        public void EDITOR_AssignParent(DrakeLodGroup group) {
            parentGroup = group;
        }

        // == IPreviewDataProvider
        public DrawMeshDatum EDITOR_GetDrawMeshDatum() {
            Mesh mesh;
            Material[] materials;
            if (Application.isPlaying == false) {
                mesh = EDITOR_GetMesh();
                materials = EDITOR_GetMaterials();
            } else {
                StartLoadingMesh();
                StartLoadingMaterials();
                mesh = WaitForCompletionMesh();
                materials = WaitForCompletionMaterials();
            }

            return new DrawMeshDatum() {
                localBounds = aabb.ToBounds(),
                layer = gameObject.layer,
                localToWorld = LocalToWorld,
                mesh = mesh,
                materials = materials,
            };
        }

        public Material EDITOR_GetMaterial(int index) {
            if (index < 0) {
                Log.Important?.Error($"Index {index} is invalid");
                return null;
            }

            if (RuntimeOverrideMaterials is { Length: > 0 }) {
                if (index >= RuntimeOverrideMaterials.Length) {
                    Log.Important?.Error($"Trying to load material at index {index} but there are only {RuntimeOverrideMaterials.Length} {nameof(RuntimeOverrideMaterials)}");
                    return null;
                }

                return RuntimeOverrideMaterials[index];
            }

            if (index >= materialReferences.Length) {
                Log.Important?.Error($"Trying to load material at index {index} but there are only {materialReferences.Length} {nameof(materialReferences)}");
                return null;
            }

            if (Application.isPlaying == false) {
                return EDITOR_LoadAsset<Material>(materialReferences[index]);
            }
            Log.Important?.Error($"You should not call {nameof(EDITOR_GetMaterial)} in playMode");

            StartLoadingMaterial(index);
            return WaitForCompletionMaterial(index);
        }

        public Material[] EDITOR_GetMaterials() {
            int count = MaterialsCountWithOverrideCheck;
            var materials = new Material[count];
            var materialsIList = (IList<Material>)materials;
            EDITOR_GetMaterials(ref materialsIList, 0);
            return materials;
        }

        public void EDITOR_GetMaterials(ref List<Material> materials) {
            int count = MaterialsCountWithOverrideCheck;
            int startIndex = materials.Count;
            for (int i = 0; i < count; i++) {
                materials.Add(null);
            }

            var materialsIList = (IList<Material>)materials;
            EDITOR_GetMaterials(ref materialsIList, startIndex);
        }

        void EDITOR_GetMaterials(ref IList<Material> materials, int startIndex) {
            if (RuntimeOverrideMaterials is { Length: > 0 }) {
                for (int i = 0; i < RuntimeOverrideMaterials.Length; i++) {
                    materials[startIndex + i] = RuntimeOverrideMaterials[i];
                }

                return;
            }

            int materialsCount = materialReferences.Length;
            if (Application.isPlaying == false) {
                for (int i = 0; i < materialsCount; i++) {
                    var materialRef = materialReferences[i];
                    if (materialRef == null) {
                        Log.Important?.Error($"DrakeMeshRenderer Material ref on index {i} is null", this);
                        materials[startIndex + i] = null;
                        continue;
                    }

                    var material = EDITOR_LoadAsset<Material>(materialRef);
                    if (material == null) {
                        Log.Important?.Error($"Failed to get Material from DrakeMeshRenderer {name} material ref at index {i} {materialReferences[i].RuntimeKey}", this);
                        materials[startIndex + i] = null;
                        continue;
                    }

                    materials[startIndex + i] = material;
                }

                return;
            }
            Log.Important?.Error($"You should not call {nameof(EDITOR_GetMaterials)} in playMode");
            StartLoadingMaterials();
            WaitForCompletionMaterials(ref materials, startIndex);
        }

        public Mesh EDITOR_GetMesh() {
            if (meshReference == null) {
                Log.Important?.Error($"Mesh reference on {nameof(DrakeMeshRenderer)} {name} is null", this);
                return null;
            }

            if (Application.isPlaying == false) {
                return EDITOR_LoadAsset<Mesh>(meshReference);
            }
            Log.Important?.Error($"You should not call {nameof(EDITOR_GetMesh)} in playMode");
            StartLoadingMesh();
            return WaitForCompletionMesh();
        }

        void OnDrawGizmosSelected() {
            if (UnityEditor.EditorPrefs.GetBool("showDrakeBounds", false)) {
                Gizmos.color = Color.green;
                var previousMatrix = Gizmos.matrix;
                Gizmos.matrix = LocalToWorld;
                Gizmos.DrawWireCube(aabb.Center, aabb.Size);
                Gizmos.matrix = previousMatrix;
                Gizmos.color = Color.blue;
                var worldAABB = WorldBounds;
                Gizmos.DrawWireCube(worldAABB.Center(), worldAABB.Size());
            }
        }
#endif
    }
}