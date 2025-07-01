using System;
using System.Runtime.CompilerServices;
using Awaken.CommonInterfaces;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UniversalProfiling;

namespace Awaken.Utility.Graphics.Mipmaps {
    [BurstCompile]
    public sealed class MipmapsStreamingMasterMaterials {
        const int InitialCapacity = 256;
        const Allocator Allocator = ARAlloc.Domain;

        static readonly UniversalProfilerMarker DumpMarker = new UniversalProfilerMarker("MipmapsStreamingMasterMaterials.Dump");
        static readonly UniversalProfilerMarker RunProvidersMarker = new UniversalProfilerMarker("MipmapsStreamingMasterMaterials.RunProviders");
        static readonly UniversalProfilerMarker RemoveMarker = new UniversalProfilerMarker("MipmapsStreamingMasterMaterials.Remove");
        static readonly UniversalProfilerMarker AddMarker = new UniversalProfilerMarker("MipmapsStreamingMasterMaterials.Add");
        static readonly UniversalProfilerMarker EarlyUpdateMarker = new UniversalProfilerMarker("MipmapsStreamingMasterMaterials.EarlyUpdate");
        static readonly UniversalProfilerMarker ExtractTexturesMarker = new UniversalProfilerMarker("MipmapsStreamingMasterMaterials.ExtractTextures");

        UnsafeHashMap<int, MaterialId> _materialToIndex;
        UnsafePinnableList<Material> _materials;
        UnsafeList<UnsafeList<MipmapsStreamingMasterTextures.TextureId>> _texturesPerMaterial;
        UnsafeList<float> _deferredMipFactors;
        UnsafeList<ushort> _refs;
        UnsafeBitmask _occupied;
        UnsafeBitmask _toRegister;

#if UNITY_EDITOR
        UnsafeAtomicSemaphore _factorsWritingSemaphore;
        UnsafeAtomicSemaphore _dumpSemaphore;
        UnsafeAtomicSemaphore _structuralChangesSemaphore;
#endif
        JobHandle _writersHandle;

        UnsafePinnableList<IMipmapsFactorProvider> _providers;

        public static MipmapsStreamingMasterMaterials Instance {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        MipmapsStreamingMasterMaterials() {
            _materialToIndex = new UnsafeHashMap<int, MaterialId>(InitialCapacity*2, Allocator);
            _materials = new UnsafePinnableList<Material>(InitialCapacity);
            _texturesPerMaterial = new(InitialCapacity, Allocator);
            _deferredMipFactors = new UnsafeList<float>(InitialCapacity, Allocator);
            _refs = new UnsafeList<ushort>(InitialCapacity, Allocator);
            _occupied = new UnsafeBitmask(InitialCapacity, Allocator);
            _toRegister = new UnsafeBitmask(InitialCapacity, Allocator);
#if UNITY_EDITOR
            _factorsWritingSemaphore = new UnsafeAtomicSemaphore(Allocator);
            _dumpSemaphore = new UnsafeAtomicSemaphore(Allocator);
            _structuralChangesSemaphore = new UnsafeAtomicSemaphore(Allocator);
#endif
            _providers = new UnsafePinnableList<IMipmapsFactorProvider>(InitialCapacity);
        }

        public static void Init() {
            if (Instance != null) { // Unity have some issues with application quit
                MipmapsStreamingMasterTextures.Instance.UnsetMipmapsStreamingMasterMaterials();
            }
            PlayerLoopUtils.RemoveFromPlayerLoop<MipmapsStreamingMasterMaterials, EarlyUpdate>();
            PlayerLoopUtils.RemoveFromPlayerLoop<MipmapsStreamingMasterMaterials, PreLateUpdate>();
            Instance = new MipmapsStreamingMasterMaterials();
            PlayerLoopUtils.RegisterToPlayerLoopBegin<MipmapsStreamingMasterMaterials, EarlyUpdate>(Instance.EarlyUpdate);
            PlayerLoopUtils.RegisterToPlayerLoopBegin<MipmapsStreamingMasterMaterials, PreLateUpdate>(Instance.RunProviders);
            MipmapsStreamingMasterTextures.Instance.SetMipmapsStreamingMasterMaterials(Instance);
        }

        public MaterialId AddMaterial(Material material) {
            AddMarker.Begin();
#if UNITY_EDITOR
            if (_factorsWritingSemaphore.Taken) {
                Log.Critical?.Error("MipmapsStreamingMasterMaterials is in writing mode");
            }
            _structuralChangesSemaphore.Take();
#endif

            if (_materialToIndex.TryGetValue(material.GetHashCode(), out var id)) {
                _refs.ElementAt(id.index)++;
#if UNITY_EDITOR
                _structuralChangesSemaphore.Release();
#endif
                AddMarker.End();
                return id;
            }

            int index;
            var firstFree = _occupied.FirstZero();
            if (_materials.Count > firstFree & firstFree != -1) {
                index = firstFree;
                _materials[index] = material;
                _deferredMipFactors.ElementAt(index) = float.MaxValue;
                _refs.ElementAt(index) = 1;
            } else {
                index = _materials.Count;
                _materials.Add(material);
                _texturesPerMaterial.Add(new UnsafeList<MipmapsStreamingMasterTextures.TextureId>(6, Allocator));
                _deferredMipFactors.Add(float.MaxValue);
                _refs.Add(1);
                _occupied.EnsureIndex((uint)index);
                _toRegister.EnsureIndex((uint)index);
            }
            id = new MaterialId(index);
            _materialToIndex.TryAdd(material.GetHashCode(), id);
            _occupied.Up((uint)index);
            _toRegister.Up((uint)index);

#if UNITY_EDITOR
            _structuralChangesSemaphore.Release();
#endif
            AddMarker.End();
            return id;
        }
        
        public void RemoveMaterial(MaterialId id) {
            RemoveMarker.Begin();
#if UNITY_EDITOR
            if (_factorsWritingSemaphore.Taken) {
                Log.Critical?.Error("MipmapsStreamingMasterMaterials is in writing mode");
            }
            if (_dumpSemaphore.Taken) {
                Log.Critical?.Error("MipmapsStreamingMasterMaterials is in dump mode");
            }
            _structuralChangesSemaphore.Take();
#endif
            var index = id.index;
            ref var refs = ref _refs.ElementAt(index);
#if UNITY_EDITOR
            if (refs == 0) {
                Log.Critical?.Error($"Material at index {id} is already removed");
                _structuralChangesSemaphore.Release();
                RemoveMarker.End();
                return;
            }
#endif
            if (--refs > 0) {
#if UNITY_EDITOR
                _structuralChangesSemaphore.Release();
#endif
                RemoveMarker.End();
                return;
            }

            var material = _materials[index];
#if UNITY_EDITOR
            if (ReferenceEquals(material, null)) {
                Log.Critical?.Error($"Material at index {id} is null");
                _structuralChangesSemaphore.Release();
                RemoveMarker.End();
                return;
            }
#endif

            _materialToIndex.Remove(material.GetHashCode());
            _occupied.Down((uint)index);
            _toRegister.Down((uint)index);
            _materials[index] = null;
            ref var textures = ref _texturesPerMaterial.ElementAt(index);
            for (int i = 0; i < textures.Length; i++) {
                MipmapsStreamingMasterTextures.Instance.RemoveTexture(textures[i]);
            }
            textures.Clear();
#if UNITY_EDITOR
            _structuralChangesSemaphore.Release();
#endif
            RemoveMarker.End();
        }

        public ParallelWriter GetParallelWriter() {
#if UNITY_EDITOR
            if (_structuralChangesSemaphore.Taken) {
                Log.Critical?.Error("MipmapsStreamingMasterMaterials is in structural changes mode");
            }
#endif
            return new ParallelWriter(this);
        }

        public void AddProvider(IMipmapsFactorProvider provider) {
            _providers.Add(provider);
        }

        public void RemoveProvider(IMipmapsFactorProvider provider) {
            _providers.Remove(provider);
        }

        public JobHandle Feed(in MipmapsStreamingMasterTextures.ParallelWriter writer) {
            DumpMarker.Begin();

            _writersHandle.Complete();
            _writersHandle = default;

#if UNITY_EDITOR
            if (_factorsWritingSemaphore.Taken) {
                Log.Critical?.Error("MipmapsStreamingMasterMaterials is in writing mode");
            }
            if(_structuralChangesSemaphore.Taken) {
                Log.Critical?.Error("MipmapsStreamingMasterMaterials is in structural changes mode");
            }
            _dumpSemaphore.Take();
#endif
            _occupied.ToIndicesOfOneArray(ARAlloc.TempJob, out var occupiedIndices);
            var jobHandle = new DumpMaterialFactorsJob {
                    occupiedIndices = occupiedIndices,
                    deferredMipFactors = _deferredMipFactors,
                    texturesPerMaterial = _texturesPerMaterial,
                    writer = writer,
                }.ScheduleParallel(occupiedIndices.LengthInt, 64, default);
            jobHandle = occupiedIndices.Dispose(jobHandle);
#if UNITY_EDITOR
            jobHandle = _dumpSemaphore.Release(jobHandle);
#endif

            DumpMarker.End();

            return jobHandle;
        }

        void EarlyUpdate() {
            EarlyUpdateMarker.Begin();
#if UNITY_EDITOR
            if (_factorsWritingSemaphore.Taken) {
                Log.Critical?.Error("MipmapsStreamingMasterMaterials is in writing mode");
            }
#endif
            var i = 5;
            foreach (var registerIndex in _toRegister.EnumerateOnes()) {
                _toRegister.Down(registerIndex);
                ExtractTextures((int)registerIndex);
                if (--i == 0) {
                    break;
                }
            }
            EarlyUpdateMarker.End();
        }

        void ExtractTextures(int index) {
            ExtractTexturesMarker.Begin();
            var material = _materials[index];
            ref var materialTextures = ref _texturesPerMaterial.ElementAt(index);
            var ids = material.GetTexturePropertyNameIDs();
            for (int i = 0; i < ids.Length; i++) {
                var texture = material.GetTexture(ids[i]);
                if (texture is Texture2D texture2D) {
                    var textureIndex = MipmapsStreamingMasterTextures.Instance.AddTexture(texture2D);
                    if (textureIndex != MipmapsStreamingMasterTextures.TextureId.Invalid) {
                        materialTextures.Add(textureIndex);
                    }
                }
            }
            ExtractTexturesMarker.End();
        }

        void RunProviders() {
            var mainCamera = CurrentCamera.Value;
            if (!mainCamera) {
#if UNITY_EDITOR
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                mainCamera = sceneView ? sceneView.camera : null;
                if (!mainCamera) {
                    return;
                }
#else
                return;
#endif
            }

            RunProvidersMarker.Begin();
            var cameraData = new CameraData(mainCamera);
            foreach (var provider in _providers) {
                provider.ProvideMipmapsFactors(cameraData, GetParallelWriter());
            }
            RunProvidersMarker.End();
        }

        public interface IMipmapsFactorProvider {
            public void ProvideMipmapsFactors(in CameraData cameraData, in ParallelWriter writer);
        }

        public struct ParallelWriter {
            UnsafeList<float> _deferredMipFactors;
#if UNITY_EDITOR
            UnsafeAtomicSemaphore _semaphore;
#endif

            public ParallelWriter(MipmapsStreamingMasterMaterials master) {
                _deferredMipFactors = master._deferredMipFactors;
#if UNITY_EDITOR
                _semaphore = master._factorsWritingSemaphore;
                _semaphore.Take();
#endif
            }

            public void UpdateMipFactor(MaterialId id, float mipFactor) {
                var index = id.index;
                mipFactor = math.min(float.MaxValue, mipFactor);
                ref var location = ref _deferredMipFactors.ElementAt(index);
                InterlockExt.Min(ref location, mipFactor);
            }

            public readonly JobHandle Dispose(JobHandle dependency) {
#if UNITY_EDITOR
                dependency = new DisposeJob {
                    _semaphore = _semaphore
                }.Schedule(dependency);
#endif
                MipmapsStreamingMasterMaterials.Instance._writersHandle =
                    JobHandle.CombineDependencies(MipmapsStreamingMasterMaterials.Instance._writersHandle,
                        dependency);
                return dependency;
            }

#if UNITY_EDITOR
            struct DisposeJob : IJob {
                [NativeDisableUnsafePtrRestriction] internal UnsafeAtomicSemaphore _semaphore;
                public void Execute() {
                    _semaphore.Release();
                }
            }
#endif
        }

        public readonly struct MaterialId : IEquatable<MaterialId> {
            public static readonly MaterialId Invalid = new MaterialId(-1);

            public readonly int index;

            public MaterialId(int index) {
                this.index = index;
            }

            public bool Equals(MaterialId other) {
                return index == other.index;
            }
            public override bool Equals(object obj) {
                return obj is MaterialId other && Equals(other);
            }
            public override int GetHashCode() {
                return index;
            }
            public static bool operator ==(MaterialId left, MaterialId right) {
                return left.Equals(right);
            }
            public static bool operator !=(MaterialId left, MaterialId right) {
                return !left.Equals(right);
            }

            public override string ToString() {
                return index.ToString();
            }
        }

        public readonly struct Accessor {
            readonly MipmapsStreamingMasterMaterials _master;

            public Accessor(MipmapsStreamingMasterMaterials master) {
                _master = master;
            }

            public UnsafePinnableList<Material> Materials => _master._materials;
            public UnsafeList<ushort> Refs => _master._refs;
            public UnsafeList<float> DeferredMipFactors => _master._deferredMipFactors;
        }

        [BurstCompile]
        struct DumpMaterialFactorsJob : IJobFor {
            [ReadOnly] public UnsafeArray<uint> occupiedIndices;
            [ReadOnly] public UnsafeList<float> deferredMipFactors;
            [ReadOnly] public UnsafeList<UnsafeList<MipmapsStreamingMasterTextures.TextureId>> texturesPerMaterial;

            [WriteOnly] public MipmapsStreamingMasterTextures.ParallelWriter writer;

            [BurstCompile]
            public void Execute(int index) {
                var materialIndex = (int)occupiedIndices[(uint)index];
                ref var mipFactor = ref deferredMipFactors.ElementAt(materialIndex);
                ref var textures = ref texturesPerMaterial.ElementAt(materialIndex);
                for (int j = 0; j < textures.Length; j++) {
                    writer.UpdateMipFactor(textures[j], mipFactor);
                }
                mipFactor = float.MaxValue;
            }
        }
    }
}
