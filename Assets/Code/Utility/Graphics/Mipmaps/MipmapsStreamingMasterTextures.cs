using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.CommonInterfaces;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UniversalProfiling;

namespace Awaken.Utility.Graphics.Mipmaps {
    public unsafe class MipmapsStreamingMasterTextures {
        const int InitialCapacity = 1024; // Measured for campaign scene
        const int MaxTexturesPerFrame = 256;
        const Allocator Allocator = ARAlloc.Domain;

        static readonly UniversalProfilerMarker EarlyUpdateMarker = new UniversalProfilerMarker("MipmapsStreamingMasterTextures.EarlyUpdate");
        static readonly UniversalProfilerMarker PostLateUpdateMarker = new UniversalProfilerMarker("MipmapsStreamingMasterTextures.PostLateUpdate");
        static readonly UniversalProfilerMarker RemoveMarker = new UniversalProfilerMarker("MipmapsStreamingMasterTextures.Remove");
        static readonly UniversalProfilerMarker AddMarker = new UniversalProfilerMarker("MipmapsStreamingMasterTextures.Add");

        MipmapsStreamingMasterMaterials _mipmapsStreamingMaterialsMaster;
        UnsafeHashMap<int, TextureId> _textureToId;
        UnsafePinnableList<Texture2D> _textures;
        UnsafeList<int> _mipmapsCounts;
        UnsafeList<int> _texelCounts;
        UnsafeList<ushort> _refs;

        UnsafeList<int> _currentMipmapsLevels;
        UnsafeList<int> _previousMipmapsLevels;

        UnsafeBitmask _occupied;

        int _mipmapsUpdateIndex;

#if UNITY_EDITOR
        public static HashSet<Texture2D> nonStreamingTextures = new HashSet<Texture2D>();
#endif

#if UNITY_EDITOR || AR_DEBUG
        int _forced = -1;
#endif

        public static MipmapsStreamingMasterTextures Instance {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        public static void Init() {
            PlayerLoopUtils.RemoveFromPlayerLoop<MipmapsStreamingMasterTextures, EarlyUpdate>();
            PlayerLoopUtils.RemoveFromPlayerLoop<MipmapsStreamingMasterTextures, PostLateUpdate>();
            Instance = new MipmapsStreamingMasterTextures();
            PlayerLoopUtils.RegisterToPlayerLoopBegin<MipmapsStreamingMasterTextures, EarlyUpdate>(Instance.EarlyUpdate);
            PlayerLoopUtils.RegisterToPlayerLoopEnd<MipmapsStreamingMasterTextures, PostLateUpdate>(Instance.PostLateUpdate);
            MipmapsStreamingMasterMaterials.Init();
        }

        MipmapsStreamingMasterTextures() {
            _textureToId = new UnsafeHashMap<int, TextureId>(InitialCapacity*2, Allocator);
            _textures = new UnsafePinnableList<Texture2D>(InitialCapacity);
            _mipmapsCounts = new UnsafeList<int>(InitialCapacity, Allocator);
            _texelCounts = new UnsafeList<int>(InitialCapacity, Allocator);
            _refs = new UnsafeList<ushort>(InitialCapacity, Allocator);
            _currentMipmapsLevels = new UnsafeList<int>(InitialCapacity, Allocator);
            _previousMipmapsLevels = new UnsafeList<int>(InitialCapacity, Allocator);
            _occupied = new UnsafeBitmask(InitialCapacity, Allocator);
        }

        public TextureId AddTexture(Texture2D texture) {
            AddMarker.Begin();

#if UNITY_EDITOR
            if (texture.isReadable) {
                Log.Minor?.Error($"Texture {texture.name} is readable. Please disable readable in import settings.", texture, LogOption.NoStacktrace);
            }
#endif

            if (!texture.streamingMipmaps) {
#if UNITY_EDITOR
                if (nonStreamingTextures.Add(texture)) {
                    Log.Minor?.Warning($"Texture {texture.name} is not set to streaming mipmaps. Please enable streaming mipmaps in the texture import settings.", texture, LogOption.NoStacktrace);
                }
#endif
                AddMarker.End();
                return TextureId.Invalid;
            }
            if (_textureToId.TryGetValue(texture.GetHashCode(), out var id)) {
                _refs.ElementAt(id.index)++;
                AddMarker.End();
                return id;
            }

            int index;
            var mipmapsCount = texture.mipmapCount;
            var texelCount = texture.width * texture.height;
            var firstFree = _occupied.FirstZero();
            if (_textures.Count > firstFree & firstFree != -1) {
                index = firstFree;
                _textures[index] = texture;
                _mipmapsCounts[index] = mipmapsCount;
                _texelCounts[index] = texelCount;
                _currentMipmapsLevels[index] = byte.MaxValue;
                _previousMipmapsLevels[index] = byte.MaxValue;
                _refs[index] = 1;
            } else {
                index = _textures.Count;
                _textures.Add(texture);
                _mipmapsCounts.Add(mipmapsCount);
                _texelCounts.Add(texelCount);
                _refs.Add(1);
                _currentMipmapsLevels.Add(byte.MaxValue);
                _previousMipmapsLevels.Add(byte.MaxValue);
                _occupied.EnsureIndex((uint)index);
            }
            _occupied.Up((uint)index);
            id = new TextureId(index);
            _textureToId.TryAdd(texture.GetHashCode(), id);
            AddMarker.End();
            return id;
        }

        public void RemoveTexture(TextureId id) {
            RemoveMarker.Begin();
            var index = id.index;
            ref var refs = ref _refs.ElementAt(index);
#if UNITY_EDITOR
            if (refs == 0) {
                Log.Critical?.Error($"Texture at index {index} is already removed");
                RemoveMarker.End();
                return;
            }
#endif
            if (--refs > 0) {
                RemoveMarker.End();
                return;
            }
            var texture = _textures[index];
            if (texture) {
                texture.requestedMipmapLevel = (byte)QualitySettings.streamingMipmapsMaxLevelReduction;
            } else {
                Log.Critical?.Error($"RemoveTexture, Texture at index {index} is null");
            }
            _textureToId.Remove(texture.GetHashCode());
            _occupied.Down((uint)index);
            _textures[index] = null;
            RemoveMarker.End();
        }

        public void SetMipmapsStreamingMasterMaterials(MipmapsStreamingMasterMaterials mipmapsStreamingMasterMaterials) {
            _mipmapsStreamingMaterialsMaster = mipmapsStreamingMasterMaterials;
        }

        public void UnsetMipmapsStreamingMasterMaterials() {
            _mipmapsStreamingMaterialsMaster = null;
        }

        void EarlyUpdate() {
            EarlyUpdateMarker.Begin();
            int clearValue = byte.MaxValue;
            UnsafeUtility.MemCpyReplicate(_currentMipmapsLevels.Ptr, &clearValue, UnsafeUtility.SizeOf<int>(), _currentMipmapsLevels.Length);
            EarlyUpdateMarker.End();
        }

        void PostLateUpdate() {
            PostLateUpdateMarker.Begin();
            var writer = new ParallelWriter {
                bias = MipmapsBias.bias,
                texelCounts = _texelCounts,
                occupied = _occupied,
                currentMipmapsLevels = _currentMipmapsLevels
            };

            JobHandle handle = _mipmapsStreamingMaterialsMaster?.Feed(writer) ?? default;

            var maxMipmapsLevel = (byte)QualitySettings.streamingMipmapsMaxLevelReduction;

            var firstIndex = math.max(_occupied.FirstOne(), 0);
            _mipmapsUpdateIndex = math.max(firstIndex, _mipmapsUpdateIndex);
            _mipmapsUpdateIndex = _mipmapsUpdateIndex >= _textures.Count ? firstIndex : _mipmapsUpdateIndex;

            handle.Complete();

            for (var i = 0; (_mipmapsUpdateIndex < _textures.Count) & (i < MaxTexturesPerFrame); i++, _mipmapsUpdateIndex++) {
                if (!_occupied[(uint)_mipmapsUpdateIndex]) {
                    continue;
                }
                var texture = _textures[_mipmapsUpdateIndex];
                var currentMipmapsLevel = _currentMipmapsLevels[_mipmapsUpdateIndex];
                currentMipmapsLevel = math.min(maxMipmapsLevel, currentMipmapsLevel);
                currentMipmapsLevel = math.min(currentMipmapsLevel, _mipmapsCounts[_mipmapsUpdateIndex]);
#if UNITY_EDITOR || AR_DEBUG
                currentMipmapsLevel = math.select(currentMipmapsLevel, _forced, _forced != -1);
#endif
                if (currentMipmapsLevel == _previousMipmapsLevels[_mipmapsUpdateIndex]) {
                    continue;
                }

                if (texture) {
                    texture.requestedMipmapLevel = currentMipmapsLevel;
                } else {
#if UNITY_EDITOR
                    Log.Critical?.Error($"UpdateTexture, Texture at index {_mipmapsUpdateIndex} is null");
#endif
                }
                _previousMipmapsLevels[_mipmapsUpdateIndex] = currentMipmapsLevel;
            }

            PostLateUpdateMarker.End();
        }

        public readonly struct TextureId : IEquatable<TextureId> {
            public static readonly TextureId Invalid = new TextureId(-1);

            public readonly int index;

            public TextureId(int index) {
                this.index = index;
            }

            public bool Equals(TextureId other) {
                return index == other.index;
            }
            public override bool Equals(object obj) {
                return obj is TextureId other && Equals(other);
            }
            public override int GetHashCode() {
                return index;
            }
            public static bool operator ==(TextureId left, TextureId right) {
                return left.Equals(right);
            }
            public static bool operator !=(TextureId left, TextureId right) {
                return !left.Equals(right);
            }
        }

        public struct ParallelWriter {
            internal float bias;
            [ReadOnly] internal UnsafeList<int> texelCounts;
            [ReadOnly] internal UnsafeBitmask occupied;
            [WriteOnly] internal UnsafeList<int> currentMipmapsLevels;

            public void UpdateMipFactor(TextureId id, float mipFactor) {
                var index = id.index;

                if (!occupied.HasOne((uint)index)) {
#if DEBUG
                    if (occupied.ElementsLength < index) {
                        Debug.LogError($"Invalid texture id {index}. Texture id is out of range.");
                    } else {
                        Debug.LogError($"Invalid texture id {index}. Texture id is not used.");
                    }
#endif
                    return;
                }

                var mipmapLevel = CalculateMipmapLevel(mipFactor, texelCounts[index]);
                UpdateMipmapsLevel(index, mipmapLevel);
            }

            void UpdateMipmapsLevel(int index, int mipmapsLevel) {
                ref var currentMipmapsLevel = ref currentMipmapsLevels.ElementAt(index);
                InterlockExt.Min(ref currentMipmapsLevel, mipmapsLevel);
            }

            byte CalculateMipmapLevel(float mipmapFactor, float texelCount) {
                if (mipmapFactor < 1e-06) {
                    return (byte)bias;
                }
                float v = texelCount * mipmapFactor;
                float desiredMipLevel = 0.5f * math.log2(v) + bias;

                return math.isfinite(desiredMipLevel) ? (byte)desiredMipLevel : (byte)255;
            }
        }

        public readonly struct Accessor {
            readonly MipmapsStreamingMasterTextures _texture;

            public UnsafePinnableList<Texture2D> Textures => _texture._textures;
            public UnsafeList<int> PreviousMipmapsLevels => _texture._previousMipmapsLevels;
            public UnsafeList<int> CurrentMipmapsLevels => _texture._currentMipmapsLevels;
            public UnsafeList<ushort> Refs => _texture._refs;
#if UNITY_EDITOR || AR_DEBUG
            public ref int Forced => ref _texture._forced;
#endif

            public Accessor(MipmapsStreamingMasterTextures texture) {
                _texture = texture;
            }
        }
    }
}
