using System;
using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Previews;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    [ExecuteAlways, SelectionBase]
    public sealed class DrakeLodGroup : MonoBehaviour, IRenderingOptimizationSystemTarget, IDrakeStaticBakeable,
        IWithOcclusionCullingTarget, IARPreviewProvider {
        [SerializeField] float lodGroupSize;
        [SerializeField] LodGroupSerializableData lodGroupSerializableData;
        [SerializeField] DrakeMeshRenderer[] children = Array.Empty<DrakeMeshRenderer>();
        [SerializeField] bool isStatic;
#if ADDRESSABLES_BUILD
        [SerializeField]
#endif
        bool _hasEntitiesAccess;
#if ADDRESSABLES_BUILD
        [SerializeField]
#endif
        bool _hasLinkedLifetime;

        public GameObject GameObject => gameObject;
        public DrakeMeshRenderer[] Renderers => children;

        public MeshLODGroupComponent MeshLODGroupComponent {
            get {
                var meshLodGroup = OverridenLodGroupSerializableData().ToLodGroupComponent();
#if UNITY_EDITOR
                var dist0 = meshLodGroup.LODDistances0;
                var dist1 = meshLodGroup.LODDistances1;
                if (!Application.isPlaying && UnityEditor.EditorPrefs.GetBool("DrakeRenderer.HighestLodMode", false)) {
                    for (int i = 0; i < 7; i++) {
                        var next = i + 1;
                        var hasNext = float.IsFinite(next < 4 ? dist0[next] : dist1[next - 4]);
                        if (hasNext) {
                            if (i < 4) {
                                dist0[i] = 0.001f * i;
                            } else {
                                dist1[i - 4] = 0.001f * i;
                            }
                        } else {
                            if (i < 4) {
                                dist0[i] = 99999999f;
                            } else {
                                dist1[i - 4] = 99999999f;
                            }
                        }
                    }
                }
                meshLodGroup.LODDistances0 = dist0;
                meshLodGroup.LODDistances1 = dist1;
#endif
                return meshLodGroup;
            }
        }

        public LodGroupSerializableData LodGroupSerializableData => OverridenLodGroupSerializableData();
        public LodGroupSerializableData LodGroupSerializableDataRaw { get => lodGroupSerializableData; set => lodGroupSerializableData = value; }
        public float LodGroupSize => lodGroupSize;
        public bool IsBaked => lodGroupSerializableData.isValid;
        public bool IsStatic => isStatic;
        public bool HasEntitiesAccess => _hasEntitiesAccess | _hasLinkedLifetime;
        public bool HasLinkedLifetime => _hasLinkedLifetime;

        void Start() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return;
            }
#endif
            Spawn();
        }

        public void Spawn() {
            var lodData = LodGroupSerializableData;
#if UNITY_EDITOR
            var dist0 = lodData.lodDistances0;
            var dist1 = lodData.lodDistances1;
            if (!Application.isPlaying && UnityEditor.EditorPrefs.GetBool("DrakeRenderer.HighestLodMode", false)) {
                for (int i = 0; i < 7; i++) {
                    var next = i + 1;
                    var hasNext = float.IsFinite(next < 4 ? dist0[next] : dist1[next - 4]);
                    if (hasNext) {
                        if (i < 4) {
                            dist0[i] = 0.001f * i;
                        } else {
                            dist1[i] = 0.001f * i;
                        }
                    }
                }
            }
            lodData.lodDistances0 = dist0;
            lodData.lodDistances1 = dist1;
#endif
            foreach (var drakeMeshRenderer in children) {
                drakeMeshRenderer.PrepareRanges(lodData.lodDistances0, lodData.lodDistances1);
            }
            DrakeRendererManager.Instance.Register(this, gameObject.scene);
        }

        public void Setup(LODGroup lodGroup, DrakeMeshRenderer[] children) {
            this.children = children;

            lodGroupSize = lodGroup.size;
            lodGroupSerializableData.Initialize(lodGroup);

            DestroyImmediate(lodGroup);
        }

        public void BakeStatic() {
            isStatic = gameObject.isStatic;
            // For safety, it's better to not bake statics than to fail building
            foreach (var drakeMeshRenderer in children ?? Array.Empty<DrakeMeshRenderer>()) {
                drakeMeshRenderer.BakeStatic();
            }
        }
        
        public void SetUnityRepresentation(in IWithUnityRepresentation.Options options) {
            if (options.linkedLifetime.HasValue) {
                _hasLinkedLifetime = options.linkedLifetime.Value;
            }
            if (options.movable.HasValue) {
                isStatic = !options.movable.Value;
            }

            if (options.requiresEntitiesAccess.HasValue) {
                _hasEntitiesAccess = options.requiresEntitiesAccess.Value;
            }
        }

        public void ClearRuntime(bool transformNeeded) {
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
            lodGroupSize = 0;
            children = Array.Empty<DrakeMeshRenderer>();
            lodGroupSerializableData = default;
        }

        LodGroupSerializableData OverridenLodGroupSerializableData() {
            return lodGroupSerializableData.WithLocalToWorldMatrix(transform.localToWorldMatrix);
        }

        // === EDITOR
#if UNITY_EDITOR
        public static Action<DrakeLodGroup> OnAddedDrakeLodGroup;
        public static Action<DrakeLodGroup> OnRemovedDrakeLodGroup;
        public static Func<DrakeLodGroup, IWithOcclusionCullingTarget.IRevertOcclusion> OnEnterOcclusionCullingCreator;

        public void OnEnable() {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
            if (IsBaked) {
                OnAddedDrakeLodGroup?.Invoke(this);
            }
        }

        public void OnDisable() {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
            OnRemovedDrakeLodGroup?.Invoke(this);
        }
#endif
        public IWithOcclusionCullingTarget.IRevertOcclusion EnterOcclusionCulling() {
#if UNITY_EDITOR && !SIMULATE_BUILD
            return OnEnterOcclusionCullingCreator?.Invoke(this) ?? IWithOcclusionCullingTarget.TargetRevertDummy;
#else
            return IWithOcclusionCullingTarget.TargetRevertDummy;
#endif
        }

        // -- AR PREVIEW
        public static Func<DrakeLodGroup, IEnumerable<IARRendererPreview>> PreviewCreator { get; set; }
        public IEnumerable<IARRendererPreview> GetPreviews() {
            return PreviewCreator?.Invoke(this);
        }
        
#if UNITY_EDITOR
        public struct EditorAccess {
            public DrakeLodGroup Value;
            public void SetLodDistancesDirect(float4x2 lodDistances) {
                Value.lodGroupSerializableData.lodDistances0 = lodDistances.c0;
                Value.lodGroupSerializableData.lodDistances1 = lodDistances.c1;
            }
            public EditorAccess(DrakeLodGroup value) {
                Value = value;
            }
        }
#endif
    }
}
