using System;
using System.Collections.Generic;
using Awaken.Kandra.AnimationPostProcessing;
using Awaken.Kandra.Data;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using MagicaCloth2;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public unsafe class RigManager : IMemorySnapshotProvider {
        static readonly int InputBonesId = Shader.PropertyToID("_InputBones");
        
        readonly int _prepareBonesKernel;
        readonly ComputeShader _prepareBonesShader;
        readonly List<KandraRig> _rigsToTrack = new List<KandraRig>();

        GraphicsBuffer _inputBonesBuffer;
        NativeArray<Bone> _inputBonesArray;
        TransformAccessArray _transformAccessArray;
        JobHandle _readTransform;

        MemoryBookkeeper _memoryRegions;
        UnsafeHashMap<int, RigData> _rigs;
        uint _bonesInFlight;

        public float FillPercentage => (float) BonesCount / _memoryRegions.Capacity;
        uint BonesCount => _memoryRegions.LastBinStart;

        public RigManager(ComputeShader prepareBonesShader) {
            var rigBonesCapacity = KandraRendererManager.FinalRigBonesCapacity;
            var maxRenderers = KandraRendererManager.FinalRenderersCapacity;

            _prepareBonesShader = prepareBonesShader;
            _prepareBonesKernel = _prepareBonesShader.FindKernel("CSPrepareBones");

            _inputBonesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, rigBonesCapacity, sizeof(Bone));
            _inputBonesArray = new NativeArray<Bone>((int)rigBonesCapacity, Allocator.Persistent);

            _transformAccessArray = new TransformAccessArray(rigBonesCapacity);
            _memoryRegions = new MemoryBookkeeper("Bones rig", (uint)rigBonesCapacity, maxRenderers/3, ARAlloc.Persistent);
            _rigs = new UnsafeHashMap<int, RigData>(maxRenderers, ARAlloc.Persistent);
        }

        public void Dispose() {
            _readTransform.Complete();
            _inputBonesBuffer?.Dispose();
            _inputBonesArray.Dispose();
            _transformAccessArray.Dispose();
            _memoryRegions.Dispose();
            _rigs.Dispose();
        }

        public bool CanRegister(KandraRig rig, out MemoryBookkeeper.MemoryRegion memoryDestination, ref string errorMessage) {
            var hash = rig.GetHashCode();

            if (_rigs.TryGetValue(hash, out var data)) {
                memoryDestination = data.memory;
                return true;
            }
            var success = _memoryRegions.FindFreeRegion((uint)rig.bones.Length, out memoryDestination);
            if (!success) {
                errorMessage = BrokenKandraMessage.AppendMessageInfo(errorMessage, (uint)rig.bones.Length, _memoryRegions);
            }
            return success;
        }

        public void RegisterRig(KandraRig rig, in MemoryBookkeeper.MemoryRegion memoryDestination) {
            var hash = rig.GetHashCode();

            if (_rigs.TryGetValue(hash, out var data)) {
                Asserts.AreEqual(data.memory, memoryDestination);
                data.refCount++;
                _rigs[hash] = data;
                return;
            }

            var bones = rig.bones;
            _memoryRegions.TakeFreeRegion(memoryDestination);

            data = new RigData {
                memory = memoryDestination,
                refCount = 1
            };
            _rigs[hash] = data;

            for (var i = 0; i < bones.Length; i++) {
                var bone = bones[i];
                var index = (int)(memoryDestination.start + i);
                Asserts.IndexInRange(index, (uint)(_transformAccessArray.length+1));
                if (_transformAccessArray.length > index) {
                    _transformAccessArray[index] = bone;
                } else {
                    _transformAccessArray.Add(bone);
                }
            }
            rig.MarkRegistered();
        }

        public void UnregisterRig(KandraRig rig) {
            var hash = rig.GetHashCode();

            if (_rigs.TryGetValue(hash, out var data)) {
                data.refCount--;
                if (data.refCount == 0) {
                    _memoryRegions.Return(data.memory);
                    _rigs.Remove(hash);
                    rig.MarkUnregistered();
                    for (int i = 0; i < data.memory.length; i++) {
                        var index = (int)(data.memory.start + i);
                        _transformAccessArray[index] = null;
                    }
                } else {
                    _rigs[hash] = data;
                }
            } else {
                Log.Important?.Error($"Trying to unregister a rig [{hash}] that was not registered.", rig);
            }
        }

        public bool CanChange(KandraRig rig, out MemoryBookkeeper.MemoryRegion memoryDestination) {
            return _memoryRegions.FindFreeRegion((uint)rig.bones.Length, out memoryDestination) && _rigs.ContainsKey(rig.GetHashCode());
        }

        public void RigChanged(KandraRig rig, in MemoryBookkeeper.MemoryRegion memoryDestination) {
            var hash = rig.GetHashCode();

            if (_rigs.TryGetValue(hash, out var oldData)) {
                _rigs.Remove(hash);

                RegisterRig(rig, memoryDestination);
                _memoryRegions.Return(oldData.memory);

                var data = _rigs[hash];
                data.refCount = oldData.refCount;
                _rigs[hash] = data;
            } else {
                Log.Important?.Error($"Trying to change a rig [{hash}] that was not registered.", rig);
            }
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            commandBuffer.SetComputeBufferParam(_prepareBonesShader, _prepareBonesKernel, InputBonesId, _inputBonesBuffer);
        }

        public void CollectBoneMatrices() {
            var bonesCount = BonesCount;
            if ((bonesCount > 0) & (_bonesInFlight == 0)) {
                var animPPHandle = AnimationPostProcessingService.JobHandle;
                var magicaCloth = MagicaManager.Cloth;
                var dependencies = magicaCloth != null ? JobHandle.CombineDependencies(animPPHandle, magicaCloth.BoneJobHandle) : animPPHandle;
                _readTransform = new ReadTransforms {
                    bonesBuffer = (Bone*)_inputBonesArray.GetUnsafePtr()
                }.ScheduleReadOnly(_transformAccessArray, 128, dependencies);
                _bonesInFlight = bonesCount;
            }
        }

        public void UnlockBuffer(CommandBuffer commandBuffer) {
            if (_bonesInFlight > 0) {
                _readTransform.Complete();
                commandBuffer.SetBufferData(_inputBonesBuffer, _inputBonesArray, 0, 0, (int)_bonesInFlight);
                _bonesInFlight = 0;
            }

            for (int i = _rigsToTrack.Count - 1; i >= 0; i--) {
                var rig = _rigsToTrack[i];
                if (rig == null) {
                    _rigsToTrack.RemoveAt(i);
                    rig.OnDestroy();
                }
            }
        }

        // There is possibility to initialized KandraRig without Awake, and then we need to track if it was destroyed as OnDestroy won't be called in such case
        public void AddRigToTrack(KandraRig kandraRig) {
            _rigsToTrack.Add(kandraRig);
        }

        public void StopRigTracking(KandraRig kandraRig) {
            _rigsToTrack.Remove(kandraRig);
        }

        public bool TryGetMemoryRegionFor(KandraRig rig, out MemoryBookkeeper.MemoryRegion region) {
            var hash = rig.GetHashCode();
            if (_rigs.TryGetValue(hash, out var data)) {
                region = data.memory;
                return true;
            }

            region = default;
            return false;
        }

        public ulong GetMemoryUsageFor(KandraRig rig) {
            var hash = rig.GetHashCode();
            if (_rigs.TryGetValue(hash, out var data)) {
                var region = data.memory;
                return region.length * (ulong)sizeof(Bone);
            }

            return 0;
        }

        struct RigData {
            public MemoryBookkeeper.MemoryRegion memory;
            public int refCount;
        }

        [BurstCompile]
        struct ReadTransforms : IJobParallelForTransform {
            [NativeDisableUnsafePtrRestriction]
            public Bone* bonesBuffer;

            public void Execute(int index, TransformAccess transform) {
                if (transform.isValid) {
                    bonesBuffer[index] = new Bone(transform.localToWorldMatrix);
                }
            }
        }

        // IMainMemorySnapshotProvider
        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 3;

            MemorySnapshotUtils.TakeSnapshot("InputBonesBuffer", _inputBonesBuffer, BonesCount, memoryBuffer.Slice(0, 1));
            MemorySnapshotUtils.TakeSnapshot("CPU InputBones", _inputBonesArray, memoryBuffer.Slice(1, 1));
            _memoryRegions.GetMemorySnapshot(memoryBuffer.Slice(2, 1));

            var rigsSelfSize = MemorySnapshotUtils.HashMapSize<int, RigData>(_rigs.Capacity);
            var rigsUsedSize = MemorySnapshotUtils.HashMapSize<int, RigData>(_rigs.Count);
            ownPlace.Span[0] = new MemorySnapshot(nameof(RigManager), rigsSelfSize, rigsUsedSize, memoryBuffer[..childrenCount]);

            return childrenCount;
        }

        public readonly struct EditorAccess {
            readonly RigManager _manager;

            public ref readonly MemoryBookkeeper BonesMemory => ref _manager._memoryRegions;

            public EditorAccess(RigManager manager) {
                _manager = manager;
            }

            public static EditorAccess Get() {
                return new EditorAccess(KandraRendererManager.Instance.RigManager);
            }
        }
    }
}