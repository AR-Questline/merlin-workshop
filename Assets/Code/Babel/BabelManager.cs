using System;
using System.Runtime.CompilerServices;
using Awaken.PackageUtilities.CommonInterfaces;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Files;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Localization;
using UniversalProfiling;

[assembly: InternalsVisibleTo("Awaken.Babel.Editor")]

namespace Awaken.Babel {
    public class BabelManager : ILocalizationManager, IMainMemorySnapshotProvider, IBabelIdBaker {
        static readonly UniversalProfilerMarker TranslateMarker = new UniversalProfilerMarker("Babel.Translate");
        static readonly UniversalProfilerMarker HasSmartFormatTagMarker = new UniversalProfilerMarker("Babel.HasSmartFormatTag");
        static readonly UniversalProfilerMarker GetTranslationAndSmartFormatTagMarker = new UniversalProfilerMarker("Babel.GetTranslationAndSmartFormatTag");
        static readonly UniversalProfilerMarker GestureMarker = new UniversalProfilerMarker("Babel.Gesture");

        UnsafeHashMap<ReadonlyString, uint> _indexById;
        UnsafeArray<char> _idsData;
        IBabelProvider _provider;

        public static IBabelIdBaker IdBaker { get; internal set; } = new FakeBabelIdBaker();

        public unsafe string Translate(string id) {
            using var marker = TranslateMarker.Auto();

            uint index;
            fixed (char* idPtr = id) {
                var readonlyString = new ReadonlyString(idPtr, (uint)id.Length);
                if (_indexById.TryGetValue(readonlyString, out index) == false) {
                    return string.Empty;
                }
            }

            return _provider.GetTranslation(index);
        }

        public string Translate(LocalizationEntryId id) {
            using var marker = TranslateMarker.Auto();

            if (id.IsValid == false) {
                return string.Empty;
            }
            return _provider.GetTranslation(id.Index);
        }

        public unsafe bool HasSmartFormatTag(string id) {
            using var marker = HasSmartFormatTagMarker.Auto();

            uint index;
            fixed (char* idPtr = id) {
                var readonlyString = new ReadonlyString(idPtr, (uint)id.Length);
                if (_indexById.TryGetValue(readonlyString, out index) == false) {
                    return false;
                }
            }
            return _provider.HasSmartFormatTag(index);
        }

        public bool HasSmartFormatTag(LocalizationEntryId id) {
            using var marker = HasSmartFormatTagMarker.Auto();

            if (id.IsValid == false) {
                return false;
            }
            return _provider.HasSmartFormatTag(id.Index);
        }

        public unsafe void GetTranslationAndSmartFormatTag(string id, out string translation, out bool smartFormatTag) {
            using var marker = GetTranslationAndSmartFormatTagMarker.Auto();

            uint index;
            fixed (char* idPtr = id) {
                var readonlyString = new ReadonlyString(idPtr, (uint)id.Length);
                if (_indexById.TryGetValue(readonlyString, out index) == false) {
                    translation = string.Empty;
                    smartFormatTag = false;
                    return;
                }
            }
            _provider.GetTranslationAndSmartFormatTag(index, out translation, out smartFormatTag);
        }

        public void GetTranslationAndSmartFormatTag(LocalizationEntryId id, out string translation, out bool smartFormatTag) {
            using var marker = GetTranslationAndSmartFormatTagMarker.Auto();

            if (id.IsValid == false) {
                translation = string.Empty;
                smartFormatTag = false;
                return;
            }
            _provider.GetTranslationAndSmartFormatTag(id.Index, out translation, out smartFormatTag);
        }

        public unsafe string GetGesture(string id) {
            using var marker = GestureMarker.Auto();

            uint index;
            fixed (char* idPtr = id) {
                var readonlyString = new ReadonlyString(idPtr, (uint)id.Length);
                if (_indexById.TryGetValue(readonlyString, out index) == false) {
                    return string.Empty;
                }
            }
            return _provider.GetGesture(index);
        }

        public string GetGesture(LocalizationEntryId id) {
            using var marker = GestureMarker.Auto();

            if (id.IsValid == false) {
                return string.Empty;
            }
            return _provider.GetGesture(id.Index);
        }

        public void Initialize() {
            LoadIds();
            if (_indexById.IsCreated) {
                IdBaker = this;
            }

            _provider = Configuration.GetBoolExact("use_preloaded_babel", true) ? new PreloadedBabelProvider() : new StreamingBabelProvider();
            _provider.Init();
            IMainMemorySnapshotProvider.RegisterProvider(this);
        }

        public void SwitchLanguage(in LocaleIdentifier locale) {
            if (_indexById.IsCreated == false) {
                return;
            }

            _provider.SwitchLanguage(locale);
        }

        unsafe void LoadIds() {
            if (BabelPersistence.TryGetBasePathForMetaLoading(out var basePath) == false) {
                Log.Critical?.Error($"Babel meta archive not found");
                return;
            }

            _idsData = FileRead.ToNewBuffer<char>(BabelPersistence.GetKeysDataPath(basePath), ARAlloc.Domain);
            var keysPositions = FileRead.ToNewBuffer<StringPosition>(BabelPersistence.GetKeysPositionsPath(basePath), ARAlloc.Temp);

            var idsDataPtr = _idsData.Ptr;
            _indexById = new UnsafeHashMap<ReadonlyString, uint>((int)(keysPositions.Length * 1.1f), ARAlloc.Domain);
            for (var i = 0u; i < keysPositions.Length; ++i) {
                ref var keyPosition = ref keysPositions[i];
                _indexById.TryAdd(new ReadonlyString(idsDataPtr + keyPosition.charStart, keyPosition.charLength), i);
            }

            keysPositions.Dispose();
        }

        public void Dispose() {
            IMainMemorySnapshotProvider.UnregisterProvider(this);
            _provider.Dispose();
            if (_idsData.IsCreated) {
                _idsData.Dispose();
                _indexById.Dispose();
            }
        }

        // === IBabelIdBaker
        public unsafe LocalizationEntryId ConvertToLocalizationEntry(string id) {
            uint index;
            fixed (char* idPtr = id) {
                var readonlyString = new ReadonlyString(idPtr, (uint)id.Length);
                if (_indexById.TryGetValue(readonlyString, out index) == false) {
                    return default;
                }
            }
            return new LocalizationEntryId(index);
        }

        // === IMainMemorySnapshotProvider
        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 1;

            var ownCapacity = sizeof(char) * _idsData.Length + MemorySnapshotUtils.HashMapSizeCapacity(_indexById);
            var ownSize = sizeof(char) * _idsData.Length + MemorySnapshotUtils.HashMapSizeInUse(_indexById);

            ownPlace.Span[0] = new MemorySnapshot("BabelManager", ownCapacity, ownSize, memoryBuffer[..childrenCount]);

            var wholeAllocation = 0;
            var children = memoryBuffer[childrenCount..];

            var allocated = _provider.GetMemorySnapshot(children, memoryBuffer.Slice(0, 1));
            wholeAllocation += allocated;
            children = children[allocated..];

            return wholeAllocation;
        }
        public int PreallocationSize => 20;

        // === Helpers
        internal readonly unsafe struct ReadonlyString : IEquatable<ReadonlyString> {
            readonly char* _data;
            readonly uint _length;

            public ReadonlyString(char* data, uint length) {
                _data = data;
                _length = length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(ReadonlyString other) {
                if (_length != other._length) {
                    return false;
                }

                return UnsafeUtility.MemCmp(_data, other._data, _length * sizeof(char)) == 0;
            }

            public override bool Equals(object obj) {
                return obj is ReadonlyString other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked {
                    var hash = (int)_length;
                    for (var i = 0; i < _length; ++i) {
                        hash = (hash * 397) ^ _data[i];
                    }
                    return hash;
                }
            }

            public static bool operator ==(ReadonlyString left, ReadonlyString right) {
                return left.Equals(right);
            }

            public static bool operator !=(ReadonlyString left, ReadonlyString right) {
                return !left.Equals(right);
            }
        }
    }

    struct StringPosition {
        public uint charStart;
        public uint charLength;
    }
}
