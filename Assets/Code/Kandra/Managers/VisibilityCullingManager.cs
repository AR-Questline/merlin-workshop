using System;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Awaken.Kandra.Managers {
    public class VisibilityCullingManager : IMemorySnapshotProvider {
        public UnsafeArray<float4x4> rootBones;
        public UnsafeArray<float> xs;
        public UnsafeArray<float> ys;
        public UnsafeArray<float> zs;
        public UnsafeArray<float> radii;
        public UnsafeArray<uint> layerMasks;
#if UNITY_EDITOR
        public UnsafeArray<ulong> sceneCullingMasks;
#endif

        public JobHandle collectCullingDataJobHandle;

        TransformAccessArray _rootBonesAccessArray;
        UnsafeArray<float4> _localBoundingSpheres;
        UnsafeArray<float4x4> _bindposes;
        uint _possibleLayers = 0;
        ulong _possibleSceneCullingLayers = 0;

        public uint PossibleLayers => _possibleLayers;
#if UNITY_EDITOR
        public ulong PossibleSceneCullingLayers => _possibleSceneCullingLayers;
#endif

        public VisibilityCullingManager() {
            var maxRenderers = (uint)KandraRendererManager.FinalRenderersCapacity;

            _rootBonesAccessArray = new TransformAccessArray((int)maxRenderers);
            _localBoundingSpheres = new UnsafeArray<float4>(maxRenderers, ARAlloc.Persistent);
            _bindposes = new UnsafeArray<float4x4>(maxRenderers, ARAlloc.Persistent);

            rootBones = new UnsafeArray<float4x4>(maxRenderers, ARAlloc.Persistent);
            xs = new UnsafeArray<float>(maxRenderers, ARAlloc.Persistent);
            ys = new UnsafeArray<float>(maxRenderers, ARAlloc.Persistent);
            zs = new UnsafeArray<float>(maxRenderers, ARAlloc.Persistent);
            radii = new UnsafeArray<float>(maxRenderers, ARAlloc.Persistent);
            layerMasks = new UnsafeArray<uint>(maxRenderers, ARAlloc.Persistent);
#if UNITY_EDITOR
            sceneCullingMasks = new UnsafeArray<ulong>(maxRenderers, ARAlloc.Persistent);
#endif
        }

        public void Dispose() {
            collectCullingDataJobHandle.Complete();
            _rootBonesAccessArray.Dispose();
            _localBoundingSpheres.Dispose();
            _bindposes.Dispose();
            rootBones.Dispose();
            xs.Dispose();
            ys.Dispose();
            zs.Dispose();
            radii.Dispose();
            layerMasks.Dispose();
#if UNITY_EDITOR
            sceneCullingMasks.Dispose();
#endif
        }

        public void Register(uint slot, in KandraRenderer.RendererData rendererData, uint layerMask
#if UNITY_EDITOR
            , ulong sceneCullingMask
#endif
            ) {
            var bindPose = rendererData.rootBoneMatrix;
            var rootBone = rendererData.rig.bones[rendererData.rootBone];
            var localBoundingSphere = rendererData.mesh.localBoundingSphere;
            localBoundingSphere.w *= rendererData.boundsAmplifier.Multiplier();

            _localBoundingSpheres[slot] = localBoundingSphere;
            _bindposes[slot] = new float4x4(
                new float4(bindPose.c0.xyz, 0),
                new float4(bindPose.c1.xyz, 0),
                new float4(bindPose.c2.xyz, 0),
                new float4(bindPose.c3.xyz, 1f)
                );

            while (_rootBonesAccessArray.length < slot) {
                _rootBonesAccessArray.Add(null);
            }

            if (_rootBonesAccessArray.length == slot) {
                _rootBonesAccessArray.Add(rootBone);
            } else {
                _rootBonesAccessArray[(int)slot] = rootBone;
            }

            var rootBoneMatrix = math.mul(rootBone.localToWorldMatrix, _bindposes[slot]);
            rootBones[slot] = rootBoneMatrix;
            var center = localBoundingSphere.xyz;
            var radius = localBoundingSphere.w;
            var worldCenter = math.mul(rootBoneMatrix, new float4(center, 1)).xyz;
            worldCenter = math.select(worldCenter, 0f, math.isnan(worldCenter) | math.isinf(worldCenter));
            var worldRadius = math.cmax(math.mul(rootBoneMatrix, new float4(radius, radius, radius, 0)).xyz);
            worldRadius = math.select(worldRadius, 1f, math.isnan(worldRadius) | math.isinf(worldRadius));
            xs[slot] = worldCenter.x;
            ys[slot] = worldCenter.y;
            zs[slot] = worldCenter.z;
            radii[slot] = worldRadius;
            layerMasks[slot] = layerMask;
#if UNITY_EDITOR
            sceneCullingMasks[slot] = sceneCullingMask;
#endif

            _possibleLayers |= layerMask;
#if UNITY_EDITOR
            _possibleSceneCullingLayers |= sceneCullingMask;
#endif
        }

        public void Unregister(uint slot) {
            _rootBonesAccessArray[(int)slot] = null;

            layerMasks[slot] = 0;
#if UNITY_EDITOR
            sceneCullingMasks[slot] = 0;
#endif

            _possibleLayers = 0;
#if UNITY_EDITOR
            _possibleSceneCullingLayers = 0;
#endif
            // TODO: Burst?
            for (var i = 0u; i < layerMasks.Length; i++) {
                _possibleLayers |= layerMasks[i];
#if UNITY_EDITOR
                _possibleSceneCullingLayers |= sceneCullingMasks[i];
#endif
            }
        }

        public void CollectCullingData(UnsafeBitmask takenSlots) {
            collectCullingDataJobHandle.Complete();
            collectCullingDataJobHandle = new CullingDataJob {
                localBoundingSpheres = _localBoundingSpheres,
                bindposes = _bindposes,
                takenSlots = takenSlots,

                boneMatrices = rootBones,
                xs = xs,
                ys = ys,
                zs = zs,
                radii = radii
            }.ScheduleReadOnly(_rootBonesAccessArray, 32);
        }

        [BurstCompile]
        struct CullingDataJob : IJobParallelForTransform {
            [ReadOnly] public UnsafeArray<float4> localBoundingSpheres;
            [ReadOnly] public UnsafeArray<float4x4> bindposes;
            [ReadOnly] public UnsafeBitmask takenSlots;

            [WriteOnly] public UnsafeArray<float4x4> boneMatrices;
            [WriteOnly] public UnsafeArray<float> xs;
            [WriteOnly] public UnsafeArray<float> ys;
            [WriteOnly] public UnsafeArray<float> zs;
            [WriteOnly] public UnsafeArray<float> radii;

            public void Execute(int index, TransformAccess transform) {
                var uIndex = (uint)index;
                if (!transform.isValid | !takenSlots[uIndex]) {
                    return;
                }

                var rootBoneMatrix = (float4x4)transform.localToWorldMatrix;
                var boneMatrix = math.mul(rootBoneMatrix, bindposes[uIndex]);
                var center = localBoundingSpheres[uIndex].xyz;
                var radius = localBoundingSpheres[uIndex].w;

                var worldCenter = math.mul(boneMatrix, new float4(center, 1)).xyz;
                worldCenter = math.select(worldCenter, 0f, math.isnan(worldCenter) | math.isinf(worldCenter));
                var worldRadius = math.length(math.mul(boneMatrix, new float4(radius, 0, 0, 0)).xyz);
                worldRadius = math.select(worldRadius, 1f, math.isnan(worldRadius) | math.isinf(worldRadius));

                boneMatrices[uIndex] = boneMatrix;
                xs[uIndex] = worldCenter.x;
                ys[uIndex] = worldCenter.y;
                zs[uIndex] = worldCenter.z;
                radii[uIndex] = worldRadius;
            }
        }

        public unsafe int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var singleElementSize = (ulong)(sizeof(float4) + sizeof(float4x4) * 2 + sizeof(float) * 4);

            var selfSize = singleElementSize * _localBoundingSpheres.Length;
            var usedSize = singleElementSize * KandraRendererManager.Instance.RegisteredRenderers;

            ownPlace.Span[0] = new MemorySnapshot("VisibilityCullingManager", selfSize, usedSize);

            return 0;
        }
    }
}