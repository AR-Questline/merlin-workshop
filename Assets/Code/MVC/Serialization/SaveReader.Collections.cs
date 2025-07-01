using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Awaken.TG.Assets.Utility.Collections;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements;

namespace Awaken.TG.MVC.Serialization {
    public partial class SaveReader {
        public void ReadNullable<T>(out T? value, NestedReader<T> reader) where T : struct {
            Read(out byte b);
            value = b switch {
                1 => reader(this),
                0 => null,
                _ => throw new Exception("Invalid byte sequence")
            };
        }

        public void ReadArray<T>(out T[] value, NestedReader<T> reader) {
            Read(out int length);
            if (length == -1) {
                value = null;
                return;
            }
            if (length == 0) {
                value = Array.Empty<T>();
                return;
            }
            value = new T[length];
            for (int i = 0; i < length; i++) {
                value[i] = reader(this);
            }
        }

        public void ReadList<T>(out List<T> value, NestedReader<T> reader) {
            Read(out int length);
            if (length == -1) {
                value = null;
                return;
            }
            value = new List<T>(length);
            for (int i = 0; i < length; i++) {
                value.Add(reader(this));
            }
        }
        
        public void ReadStructList<T>(out StructList<T> value, NestedReader<T> reader) {
            Read(out int length);
            value = new StructList<T>(length);
            for (int i = 0; i < length; i++) {
                value.Add(reader(this));
            }
        }
        
        public void ReadFrugalList<T>(out FrugalList<T> value, NestedReader<T> reader) {
            Read(out int length);
            value = new FrugalList<T>();
            for (int i = 0; i < length; i++) {
                value.Add(reader(this));
            }
        }
        
        public void ReadUnsafePinnableList<T>(out UnsafePinnableList<T> list, NestedReader<T> reader) {
            Read(out int length);
            if (length == -1) {
                list = null;
                return;
            }
            list = new UnsafePinnableList<T>(length);
            list.Count = length;
            for (int i = 0; i < length; i++) {
                list[i] = reader(this);
            }
        }

        public void ReadArraySparse<T>(out T[] array, NestedReader<T> reader) {
            Read(out int length);
            if (length == -1) {
                array = null;
                return;
            }
            array = new T[length];
            while (true) {
                Read(out int index);
                if (index == -1) {
                    break;
                }
                array[index] = reader(this);
            }
        }

        public void ReadHashSet<T>(out HashSet<T> value, NestedReader<T> reader) {
            Read(out int length);
            if (length == -1) {
                value = null;
                return;
            }
            value = new HashSet<T>(length);
            for (int i = 0; i < length; i++) {
                value.Add(reader(this));
            }
        }

        public void ReadDictionary<TKey, TValue>(out Dictionary<TKey, TValue> value, NestedReader<TKey> keyReader, NestedReader<TValue> valueReader) {
            Read(out int length);
            if (length == -1) {
                value = null;
                return;
            }
            value = new Dictionary<TKey, TValue>(length);
            for (int i = 0; i < length; i++) {
                var key = keyReader(this);
                var val = valueReader(this);
                value.Add(key, val);
            }
        }
        public void ReadMultiMap<TKey, TValue>(out MultiMap<TKey, TValue> value, NestedReader<TKey> keyReader, NestedReader<TValue> valueReader) {
            Read(out int length);
            if (length == -1) {
                value = null;
                return;
            }
            value = new MultiMap<TKey, TValue>(length);
            for (int i = 0; i < length; i++) {
                var key = keyReader(this);
                ReadHashSet(out var val, valueReader);
                value.Add(key, val);
            }
        }
        public void ReadConcurrentDictionary<TKey, TValue>(out ConcurrentDictionary<TKey, TValue> value, NestedReader<TKey> keyReader, NestedReader<TValue> valueReader) {
            Read(out int length);
            if (length == -1) {
                value = null;
                return;
            }
            value = new ConcurrentDictionary<TKey, TValue>();
            for (int i = 0; i < length; i++) {
                var key = keyReader(this);
                var val = valueReader(this);
                value.TryAdd(key, val);
            }
        }
        public void ReadMultiTypeDictionary<TKey>(out MultiTypeDictionary<TKey> value, NestedReader<TKey> keyReader) {
            ReadDictionary(out Dictionary<TKey, object> dictionary, keyReader, static reader => {
                reader.Read(out byte type);
                switch (type) {
                    case 0: 
                        return null;
                    case 1:
                        reader.Read(out bool @bool);
                        return @bool;
                    case 2:
                        reader.Read(out int @int);
                        return @int;
                    case 3:
                        reader.Read(out float @float);
                        return @float;
                    case 4:
                        reader.Read(out string @string);
                        return @string;
                    case 5:
                        reader.Read(out QuestState questState);
                        return questState;
                    case 6:
                        reader.Read(out ObjectiveState objectiveState);
                        return objectiveState;
                    default:
                        throw new Exception($"Not supported type {type} in MultiTypeDictionary");
                }
            });
            value = new MultiTypeDictionary<TKey>(dictionary);
        }
        
        public delegate T NestedReader<out T>(SaveReader reader);
    }
}