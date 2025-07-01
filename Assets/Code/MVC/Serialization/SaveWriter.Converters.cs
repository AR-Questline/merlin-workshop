using System;
using System.Text;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums.Helpers;
using Awaken.Utility.LowLevel.Collections;
using FMODUnity;
using Awaken.Utility.Serialization;
using Unity.Collections;

namespace Awaken.TG.MVC.Serialization {
    public partial class SaveWriter {
        public void Write(UnicodeString unicodeString) {
            string asString = unicodeString;
            var bytes = asString == null ? Array.Empty<byte>() : Encoding.Unicode.GetBytes(asString);
            WriteArray(bytes, static (writer, b) => writer.Write(b));
        }
        
        public void Write<T>(WeakModelRef<T> weakModelRef) where T : class, IModel {
            WriteAscii(weakModelRef.id);
        }

        public void Write(in ModelElements modelElements) {
            var elements = ModelElements.Access.Elements(modelElements);
            if (elements == null) {
                Write(0);
                return;
            }
            
            int count = 0;
            foreach (var element in elements) {
                if (element.IsBeingSaved) {
                    ++count;
                }
            }
            if (count == 0) {
                Write(0);
                return;
            }
            
            Write(count);
            foreach (var element in elements) {
                if (element.IsBeingSaved) {
                    WriteModel(element);
                }
            }
        }

        public void Write(Relation relation) {
            WriteAsciiNoEnd(StaticStringSerialization.TypeName(relation.Pair.DeclaringType));
            WriteAsciiNoEnd(":");
            WriteAscii(relation.Name);
        }

        public void Write(LargeFileIndex largeFileIndex) {
            Write(largeFileIndex.value);
            World.Services.Get<LargeFilesStorage>().AddUsedLargeFile(largeFileIndex);
        }
        
        public void Write(ItemInSlots itemInSlots) {
            var items = ItemInSlots.SerializationAccess.ItemsBySlot(ref itemInSlots);
            WriteArraySparse(items, static (writer, item) => writer.WriteModel(item));
        }
        
        public void Write(EventReference fmodEvent) {
            Write(fmodEvent.Guid);
        }

        public unsafe void Write(UnsafeBitmask bitmask) {
            if (bitmask.IsCreated) {
                if (UnsafeBitmask.SerializationAccess.Allocator(ref bitmask) != ARAlloc.Persistent) {
                    throw new NotSupportedException("Only Persistent bitmasks are supported");
                }
                var length = UnsafeBitmask.SerializationAccess.Length(ref bitmask);
                var ptr = UnsafeBitmask.SerializationAccess.Ptr(ref bitmask);
                var buckets = bitmask.BucketsLength;
                Write(length);
                for (int i = 0; i < buckets; i++) {
                    Write(ptr[i]);
                }
            } else {
                Write(0u);
            }
        }

        public unsafe void Write(UnsafeSparseBitmask bitmask) {
            if (bitmask.IsCreated) {
                if (UnsafeSparseBitmask.SerializationAccess.Allocator(ref bitmask) != ARAlloc.Persistent) {
                    throw new NotSupportedException("Only Persistent bitmasks are supported");
                }
                var rangesCount = UnsafeSparseBitmask.SerializationAccess.RangesCount(ref bitmask);
                var bucketsCount = UnsafeSparseBitmask.SerializationAccess.RangedBucketsCount(ref bitmask);
                var ranges = UnsafeSparseBitmask.SerializationAccess.Ranges(ref bitmask);
                var buckets = UnsafeSparseBitmask.SerializationAccess.RangedBuckets(ref bitmask);
                Write(rangesCount);
                Write(bucketsCount);
                for (int i = 0; i < rangesCount; i++) {
                    Write(ranges[i]);
                }
                for (int i = 0; i < bucketsCount; i++) {
                    Write(buckets[i]);
                }
            } else {
                Write(0u);
                Write(0u);
            }
        }
        
        public void Write(ARAssetReference assetReference) {
            if (assetReference == null) {
                WriteAscii(null);
                return;
            }
            WriteAscii(assetReference.Address);
            WriteAscii(assetReference.SubObjectName);
        }

        public void Write(ShareableARAssetReference assetReference) {
            if (assetReference == null) {
                WriteAscii(null);
                return;
            }
            WriteAscii(assetReference.AssetGUID);
            WriteAscii(assetReference.SubObject);
        }
    }
}