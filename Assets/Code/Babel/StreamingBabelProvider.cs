using System;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Files;
using Awaken.Utility.LowLevel.Collections;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine.Localization;

namespace Awaken.Babel {
    public class StreamingBabelProvider : IBabelProvider {
        Optional<LocaleData> _localeData;
        Optional<MetaGestureData> _gestureData;

        public void Init() {
            LoadGestures();
        }

        public void Dispose() {
            if (_localeData.TryGetValue(out var oldData)) {
                oldData.Dispose();
            }
            _localeData = default;

            if (_gestureData.TryGetValue(out var oldMetaData)) {
                oldMetaData.Dispose();
            }
            _gestureData = default;
        }

        public void SwitchLanguage(in LocaleIdentifier locale) {
            if (_localeData.TryGetValue(out var oldData)) {
                oldData.Dispose();
            }

            _localeData = default;

            if (BabelPersistence.TryGetBasePathForLanguageLoading(locale, out var basePath) == false) {
                return;
            }

            var newLocaleData = new LocaleData();

            newLocaleData.stringsFileHandle = AsyncReadManager.OpenFileAsync(BabelPersistence.GetStringsPath(basePath));
            newLocaleData.positions = FileRead.ToNewBuffer<StringPosition>(BabelPersistence.GetPositionsPath(basePath), ARAlloc.Persistent);

            var tagsAsArray = FileRead.ToNewBuffer<ulong>(BabelPersistence.GetSmartTagsPath(basePath), ARAlloc.Persistent);
            newLocaleData.smartFormatTags = new UnsafeBitmask(newLocaleData.positions.Length, tagsAsArray);

            _localeData = newLocaleData;
        }

        public string GetTranslation(uint index) {
            if (_localeData.TryGetValue(out var data) == false) {
                return string.Empty;
            }
            return GetTranslation(index, ref data);
        }

        public bool HasSmartFormatTag(uint index) {
            if (_localeData.TryGetValue(out var data) == false) {
                return false;
            }
            return data.smartFormatTags[index];
        }

        public void GetTranslationAndSmartFormatTag(uint idIndex, out string translation, out bool smartFormatTag) {
            if (_localeData.TryGetValue(out var data) == false) {
                translation = string.Empty;
                smartFormatTag = false;
                return;
            }
            translation = GetTranslation(idIndex, ref data);
            smartFormatTag = data.smartFormatTags[idIndex];
        }

        public string GetGesture(uint index) {
            if (_gestureData.TryGetValue(out var data) == false) {
                return string.Empty;
            }
            return GetMetaGesture(index, ref data);
        }

        void LoadGestures() {
            if (BabelPersistence.TryGetBasePathForMetaLoading(out var basePath) == false) {
                Log.Critical?.Error($"Babel meta archive not found");
                return;
            }

            var newGestureData = new MetaGestureData();

            newGestureData.stringsFileHandle = AsyncReadManager.OpenFileAsync(BabelPersistence.GetGesturesDataPath(basePath));
            newGestureData.positions = FileRead.ToNewBuffer<StringPosition>(BabelPersistence.GetGesturesPositionsPath(basePath), ARAlloc.Persistent);

            _gestureData = newGestureData;
        }

        static unsafe string GetTranslation(uint index, ref LocaleData data) {
            ref var position = ref data.positions[index];
            var stringBuffer = FileRead.ToNewBuffer<char>(data.stringsFileHandle, position.charStart * sizeof(char), position.charLength, ARAlloc.Temp);
            var output = new string(stringBuffer.Ptr, 0, (int)position.charLength);
            stringBuffer.Dispose();
            return output;
        }

        static unsafe string GetMetaGesture(uint index, ref MetaGestureData data) {
            ref var position = ref data.positions[index];
            var stringBuffer = FileRead.ToNewBuffer<char>(data.stringsFileHandle, position.charStart * sizeof(char), position.charLength, ARAlloc.Temp);
            var output = new string(stringBuffer.Ptr, 0, (int)position.charLength);
            stringBuffer.Dispose();
            return output;
        }

        struct LocaleData : IMemorySnapshotProvider {
            public FileHandle stringsFileHandle;
            public UnsafeArray<StringPosition> positions;
            public UnsafeBitmask smartFormatTags;

            public void Dispose() {
                stringsFileHandle.Close().Complete();
                positions.Dispose();
                smartFormatTags.Dispose();
            }

            public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
                var childrenCount = 2;

                ownPlace.Span[0] = new MemorySnapshot("Locale data", 0, 0, memoryBuffer[..childrenCount]);

                MemorySnapshotUtils.TakeSnapshot<StringPosition>("Positions", positions, memoryBuffer.Slice(0, 1));
                smartFormatTags.GetMemorySnapshot("Smart tags", memoryBuffer.Slice(1, 1));

                return childrenCount;
            }
        }

        struct MetaGestureData : IMemorySnapshotProvider {
            public FileHandle stringsFileHandle;
            public UnsafeArray<StringPosition> positions;

            public void Dispose() {
                stringsFileHandle.Close().Complete();
                positions.Dispose();
            }

            public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
                var childrenCount = 1;

                ownPlace.Span[0] = new MemorySnapshot("Gestures data", 0, 0, memoryBuffer[..childrenCount]);

                MemorySnapshotUtils.TakeSnapshot<StringPosition>("Positions", positions, memoryBuffer.Slice(0, 1));

                return childrenCount;
            }
        }

        // === Memory snapshot
        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 2;


            ownPlace.Span[0] = new MemorySnapshot("Streaming babel", 0, 0, memoryBuffer[..childrenCount]);

            var wholeAllocation = 0;
            var children = memoryBuffer[childrenCount..];

            if (_localeData.TryGetValue(out var localeData)) {
                var allocated = localeData.GetMemorySnapshot(children, memoryBuffer.Slice(0, 1));
                wholeAllocation += allocated;
                children = children[allocated..];
            } else {
                memoryBuffer.Slice(0, 1).Span[0] = new MemorySnapshot("Locale data not initialized", 0, 0);
            }

            if (_gestureData.TryGetValue(out var gestureData)) {
                var allocated = gestureData.GetMemorySnapshot(children, memoryBuffer.Slice(1, 1));
                wholeAllocation += allocated;
                children = children[allocated..];
            } else {
                memoryBuffer.Slice(1, 1).Span[0] = new MemorySnapshot("Gesture data not initialized", 0, 0);
            }

            return wholeAllocation;
        }
    }
}
