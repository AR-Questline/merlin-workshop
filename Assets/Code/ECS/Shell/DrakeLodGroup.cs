using System;
using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.Utility.Previews;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public sealed class DrakeLodGroup : MonoBehaviour, IRenderingOptimizationSystemTarget, IDrakeStaticBakeable,
        IWithOcclusionCullingTarget, IARPreviewProvider {
        public GameObject GameObject;
        public DrakeMeshRenderer[] Renderers = Array.Empty<DrakeMeshRenderer>();

        public MeshLODGroupComponent MeshLODGroupComponent => throw new NotImplementedException();

        public LodGroupSerializableData LodGroupSerializableData;
        public LodGroupSerializableData LodGroupSerializableDataRaw;
        public float LodGroupSize;
        public bool IsBaked;
        public bool HasEntitiesAccess;
        public bool HasLinkedLifetime;

        public void Spawn() {
            throw new NotImplementedException();
        }

        public void Setup(LODGroup lodGroup, DrakeMeshRenderer[] children) {
            throw new NotImplementedException();
        }

        public bool IsStatic { get; }

        public void BakeStatic() {
            throw new NotImplementedException();
        }

        public void SetUnityRepresentation(in IWithUnityRepresentation.Options options) {
            throw new NotImplementedException();
        }

        public void ClearRuntime(bool transformNeeded) {
            throw new NotImplementedException();
        }

        public void ClearData() {
            throw new NotImplementedException();
        }

        public static Action<DrakeLodGroup> OnAddedDrakeLodGroup;
        public static Action<DrakeLodGroup> OnRemovedDrakeLodGroup;
        public static Func<DrakeLodGroup, IWithOcclusionCullingTarget.IRevertOcclusion> OnEnterOcclusionCullingCreator;

        public void OnEnable() {
            throw new NotImplementedException();
        }

        public void OnDisable() {
            throw new NotImplementedException();
        }

        public IWithOcclusionCullingTarget.IRevertOcclusion EnterOcclusionCulling() {
            throw new NotImplementedException();
        }

        public static Func<DrakeLodGroup, IEnumerable<IARRendererPreview>> PreviewCreator { get; set; }

        public IEnumerable<IARRendererPreview> GetPreviews() {
            throw new NotImplementedException();
        }

        public struct EditorAccess {
            public DrakeLodGroup Value;

            public void SetLodDistancesDirect(float4x2 lodDistances) {
                throw new NotImplementedException();
            }

            public EditorAccess(DrakeLodGroup value) {
                throw new NotImplementedException();
            }
        }
    }
}