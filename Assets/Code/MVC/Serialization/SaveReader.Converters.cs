using System;
using System.Collections.Generic;
using System.Text;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using FMODUnity;
using Awaken.Utility.Serialization;
using Unity.Collections;

namespace Awaken.TG.MVC.Serialization {
    public partial class SaveReader {
        public void Read(out UnicodeString unicodeString) {
            ReadArray(out byte[] bytes, static reader => { reader.Read(out byte b); return b; });
            unicodeString = new UnicodeString(Encoding.Unicode.GetString(bytes));
        }

        public void Read<T>(out WeakModelRef<T> weakModelRef) where T : class, IModel {
            ReadAscii(out weakModelRef.id);
        }

        public void Read(out ModelElements modelElements) {
            Read(out int length);
            if (length == 0) {
                modelElements = new ModelElements((List<Element>)null);
                return;
            }
            var elements = new List<Element>(length);
            for (int i = 0; i < length; i++) {
                ReadModel(out Element element);
                elements.Add(element);
            }
            modelElements = new ModelElements(elements);
        }

        public void Read(out Relation relation) {
            ReadAscii(out var serialized);
            if (string.IsNullOrEmpty(serialized)) {
                relation = null;
            } else {
                relation = Relation.Deserialize(serialized);
            }
        }

        public void Read(out LargeFileIndex largeFileIndex) {
            Read(out int index);
            largeFileIndex = new LargeFileIndex(index);
        }
        
        public void Read(out ItemInSlots itemInSlots) {
            ReadArraySparse(out Item[] items, static reader => {
                reader.ReadModel(out Item item);
                return item;
            });
            itemInSlots = new ItemInSlots(items);
        }

        public void Read(out EventReference fmodEvent) {
            Read(out fmodEvent.Guid);
#if UNITY_EDITOR
            fmodEvent.Path = null;
#endif
        }

        public unsafe void Read(out UnsafeBitmask bitmask) {
            Read(out uint length);
            if (length == 0) {
                bitmask = new UnsafeBitmask(0u, Allocator.Persistent);
            } else {
                bitmask = new UnsafeBitmask(length, ARAlloc.Persistent);
                var buckets = bitmask.BucketsLength;
                var ptr = UnsafeBitmask.SerializationAccess.Ptr(ref bitmask);
                for (int i = 0; i < buckets; i++) {
                    Read(out ptr[i]);
                }
            }
        }

        public unsafe void Read(out UnsafeSparseBitmask bitmask) {
            Read(out uint rangesCount);
            Read(out uint bucketsCount);
            if (rangesCount == 0 && bucketsCount == 0) {
                bitmask = new UnsafeSparseBitmask(ARAlloc.Persistent);
            } else {
                bitmask = new UnsafeSparseBitmask(ARAlloc.Persistent, rangesCount, bucketsCount);
                var ranges = UnsafeSparseBitmask.SerializationAccess.Ranges(ref bitmask);
                var buckets = UnsafeSparseBitmask.SerializationAccess.RangedBuckets(ref bitmask);
                for (int i = 0; i < rangesCount; i++) {
                    Read(out ranges[i]);
                }
                for (int i = 0; i < bucketsCount; i++) {
                    Read(out buckets[i]);
                }
            }
        }

        public void Read(out ARAssetReference assetReference) {
            ReadAscii(out var guid);
            if (string.IsNullOrEmpty(guid)) {
                assetReference = new ARAssetReference("");
                return;
            }
            ReadAscii(out var subObject);
            assetReference = new ARAssetReference(guid, subObject);
        }

        public void Read(out ShareableARAssetReference assetReference) {
            ReadAscii(out var guid);
            if (string.IsNullOrEmpty(guid)) {
                assetReference = new ShareableARAssetReference(new ARAssetReference(""));
                return;
            }
            ReadAscii(out var subObject);
            assetReference = new ShareableARAssetReference(new ARAssetReference(guid, subObject));
        }
    }
}