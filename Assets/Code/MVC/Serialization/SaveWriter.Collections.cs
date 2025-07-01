using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Awaken.TG.Assets.Utility.Collections;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;

namespace Awaken.TG.MVC.Serialization {
    public partial class SaveWriter {
        public void WriteNullable<T>(T? value, NestedWriter<T> writer) where T : struct {
            if (value.HasValue) {
                WriteByte(1);
                writer(this, value.Value);
            } else {
                WriteByte(0);
            }
        }

        public void WriteArray<T>(T[] array, NestedWriter<T> writer) {
            if (array == null) {
                Write(-1);
                return;
            }
            Write(array.Length);
            for (int i = 0; i < array.Length; i++) {
                writer(this, array[i]);
            }
        }

        public void WriteList<T>(List<T> value, NestedWriter<T> writer) {
            if (value == null) {
                Write(-1);
                return;
            }
            Write(value.Count);
            for (int i = 0; i < value.Count; i++) {
                writer(this, value[i]);
            }
        }
        
        public void WriteStructList<T>(in StructList<T> value, NestedWriter<T> writer) {
            Write(value.Count);
            for (int i = 0; i < value.Count; i++) {
                writer(this, value[i]);
            }
        }

        public void WriteFrugalList<T>(FrugalList<T> value, NestedWriter<T> writer) {
            Write(value.Count);
            for (int i = 0; i < value.Count; i++) {
                writer(this, value[i]);
            }
        }

        public void WriteUnsafePinnableList<T>(UnsafePinnableList<T> list, NestedWriter<T> writer) {
            if (list == null) {
                Write(-1);
                return;
            }
            Write(list.Count);
            for (int i = 0; i < list.Count; i++) {
                writer(this, list[i]);
            }
        }
        
        public void WriteArraySparse<T>(T[] array, NestedWriter<T> writer) where T : class {
            if (array == null) {
                Write(-1);
                return;
            }
            Write(array.Length);
            for (int i = 0; i < array.Length; i++) {
                if (array[i] != null) {
                    Write(i);
                    writer(this, array[i]);
                }
            }
            Write(-1);
        }

        public void WriteHashSet<T>(HashSet<T> value, NestedWriter<T> writer) {
            if (value == null) {
                Write(-1);
                return;
            }
            Write(value.Count);
            foreach (var item in value) {
                writer(this, item);
            }
        }

        public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> value, NestedWriter<TKey> keyWriter, NestedWriter<TValue> valueWriter) {
            if (value == null) {
                Write(-1);
                return;
            }
            Write(value.Count);
            foreach (var kvp in value) {
                keyWriter(this, kvp.Key);
                valueWriter(this, kvp.Value);
            }
        }

        public void WriteMultiMap<TKey, TValue>(MultiMap<TKey, TValue> value, NestedWriter<TKey> keyWriter, NestedWriter<TValue> valueWriter) {
            if (value == null) {
                Write(-1);
                return;
            }
            Write(value.Count);
            foreach (var kvp in value) {
                keyWriter(this, kvp.Key);
                WriteHashSet(kvp.Value, valueWriter);
            }
        }

        public void WriteConcurrentDictionary<TKey, TValue>(ConcurrentDictionary<TKey, TValue> value, NestedWriter<TKey> keyWriter, NestedWriter<TValue> valueWriter) {
            if (value == null) {
                Write(-1);
                return;
            }
            Write(value.Count);
            foreach (var kvp in value) {
                keyWriter(this, kvp.Key);
                valueWriter(this, kvp.Value);
            }
        }

        public void WriteMultiTypeDictionary<TKey>(MultiTypeDictionary<TKey> value, NestedWriter<TKey> keyWriter) {
            WriteDictionary(value.dictionary, keyWriter, static (writer, val) => {
                switch (val) {
                    case null:
                        writer.WriteByte(0);
                        break;
                    case bool @bool:
                        writer.WriteByte(1);
                        writer.Write(@bool);
                        break;
                    case int @int:
                        writer.WriteByte(2);
                        writer.Write(@int);
                        break;
                    case float @float:
                        writer.WriteByte(3);
                        writer.Write(@float);
                        break;
                    case string @string:
                        writer.WriteByte(4);
                        writer.Write(@string);
                        break;
                    case QuestState questState:
                        writer.WriteByte(5);
                        writer.Write(questState);
                        break;
                    case ObjectiveState objectiveState:
                        writer.WriteByte(6);
                        writer.Write(objectiveState);
                        break;
                    default:
                        throw new Exception($"Not supported type {val.GetType()} in MultiTypeDictionary");
                }
            });
        }
        
        public delegate void NestedWriter<in T>(SaveWriter writer, T value);
    }
}