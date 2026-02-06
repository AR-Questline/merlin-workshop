using System.Collections.Generic;
using System.IO;
using Awaken.PackageUtilities.CommonInterfaces;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using UnityEngine.Localization;

namespace Awaken.Babel.Editor {
    public class BabelBaker : IBabelIdBaker {
        Dictionary<string, uint> _orderedIds;

        public BabelBaker(Dictionary<string, uint> allIds) {
            _orderedIds = allIds;
            BakeIds();

            BabelManager.IdBaker = this;
        }

        public unsafe void BakeLocale(in LocaleIdentifier locale, BabelLanguageInputDatum[] inputData) {
            BabelPersistence.TryGetBasePathForLanguageLoading(locale, out var basePath);

            Directory.CreateDirectory(basePath);

            var stringsWriter = new FileWriter(BabelPersistence.GetStringsPath(basePath));
            var positionsWriter = new FileWriter(BabelPersistence.GetPositionsPath(basePath));

            var tags = new UnsafeBitmask((uint)inputData.Length, ARAlloc.Temp);

            var translationsPosition = 0u;
            for (var i = 0u; i < inputData.Length; ++i) {
                var inputDatum = inputData[i];

                fixed (char* translationPtr = inputDatum.translation) {
                    stringsWriter.Write(translationPtr, inputDatum.translation.Length);
                }

                var position = new StringPosition {
                    charStart = translationsPosition,
                    charLength = (uint)inputDatum.translation.Length
                };
                positionsWriter.Write(position);

                translationsPosition += position.charLength;

                tags[i] = inputDatum.smartFormatTag;
            }
            stringsWriter.Dispose();
            positionsWriter.Dispose();

            var smartTagsWriter = new FileWriter(BabelPersistence.GetSmartTagsPath(basePath));
            ref var tagsMask = ref UnsafeBitmask.SerializationAccess.Ptr(ref tags);
            smartTagsWriter.Write(tagsMask, tags.BucketsLength);
            smartTagsWriter.Dispose();
            tags.Dispose();
        }

        public unsafe void BakeMeta(BabelMetaInputDatum[] inputData) {
            BabelPersistence.TryGetBasePathForMetaLoading(out var basePath);

            var gesturesDataWriter = new FileWriter(BabelPersistence.GetGesturesDataPath(basePath));
            var gesturesPositionWriter = new FileWriter(BabelPersistence.GetGesturesPositionsPath(basePath));

            var keysPosition = 0u;
            for (var i = 0u; i < inputData.Length; ++i) {
                var inputDatum = inputData[i];

                fixed (char* gesturePtr = inputDatum.gesture) {
                    gesturesDataWriter.Write(gesturePtr, inputDatum.gesture.Length);
                }

                var position = new StringPosition {
                    charStart = keysPosition,
                    charLength = (uint)inputDatum.gesture.Length
                };
                gesturesPositionWriter.Write(position);

                keysPosition += position.charLength;
            }
            gesturesDataWriter.Dispose();
            gesturesPositionWriter.Dispose();
        }

        public LocalizationEntryId ConvertToLocalizationEntry(string id) {
            return _orderedIds.TryGetValue(id, out var index) ? new LocalizationEntryId(index) : default;
        }

        unsafe void BakeIds() {
            BabelPersistence.TryGetBasePathForMetaLoading(out var basePath);

            if (Directory.Exists(basePath)) {
                Directory.Delete(basePath, true);
            }
            Directory.CreateDirectory(basePath);

            var keysDataWriter = new FileWriter(BabelPersistence.GetKeysDataPath(basePath));
            var keysPositionWriter = new FileWriter(BabelPersistence.GetKeysPositionsPath(basePath));

            var keysPosition = 0u;

            var ids = new string[_orderedIds.Count];
            foreach (var (id, index) in _orderedIds) {
                ids[index] = id;
            }

            for (var i = 0u; i < ids.Length; ++i) {
                var id = ids[i];

                fixed (char* idPtr = id) {
                    keysDataWriter.Write(idPtr, id.Length);
                }

                var position = new StringPosition {
                    charStart = keysPosition,
                    charLength = (uint)id.Length
                };
                keysPositionWriter.Write(position);

                keysPosition += position.charLength;
            }
            keysDataWriter.Dispose();
            keysPositionWriter.Dispose();
        }
    }

    public readonly struct BabelMetaInputDatum {
        public static BabelMetaInputDatum Empty => new BabelMetaInputDatum(string.Empty, string.Empty);

        public readonly string id;
        public readonly string gesture;

        public BabelMetaInputDatum(string id, string gesture) {
            this.id = id;
            this.gesture = gesture;
        }
    }

    public readonly struct BabelLanguageInputDatum {
        public static BabelLanguageInputDatum Empty => new BabelLanguageInputDatum(string.Empty, string.Empty, false);

        public readonly string id;
        public readonly string translation;
        public readonly bool smartFormatTag;

        public BabelLanguageInputDatum(string id, string translation, bool smartFormatTag) {
            this.id = id;
            this.translation = translation;
            this.smartFormatTag = smartFormatTag;
        }
    }
}
