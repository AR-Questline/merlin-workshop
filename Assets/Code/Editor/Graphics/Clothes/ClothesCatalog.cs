using System;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics.Clothes {
    public class ClothesCatalog : ScriptableObject {
        const string SingletonGuid = "e213844d45162374a9b373f0c482b475";

        public static ClothesCatalog Instance => AssetDatabase.LoadAssetAtPath<ClothesCatalog>(AssetDatabase.GUIDToAssetPath(SingletonGuid));

        public ClothesCategory[] categories = Array.Empty<ClothesCategory>();

        public int FindDefaultCategory(string clothName) {
            for (int i = 0; i < categories.Length; i++) {
                if (clothName.Contains(categories[i].name)) {
                    return i;
                }
            }

            return -1;
        }

        public bool Has(GameObject clothPrefab) {
            var clothGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clothPrefab));

            foreach (var catalogCategory in categories) {
                foreach (var cloth in catalogCategory.clothes) {
                    if (cloth.guid == clothGuid) {
                        return true;
                    }
                }
            }

            return false;
        }

        [Serializable]
        public struct ClothesCategory {
            public string name;
            public Cloth[] clothes;
        }

        [Serializable]
        public struct Cloth : IEquatable<Cloth> {
            public string guid;
            [ShowInInspector] public GameObject Prefab {
                get => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                set => guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
            }
            public bool Equals(Cloth other) {
                return guid == other.guid;
            }
            public override bool Equals(object obj) {
                return obj is Cloth other && Equals(other);
            }
            public override int GetHashCode() {
                return guid.GetHashCode();
            }
            public static bool operator ==(Cloth left, Cloth right) {
                return left.Equals(right);
            }
            public static bool operator !=(Cloth left, Cloth right) {
                return !left.Equals(right);
            }
        }

        // === Add
        [ShowInInspector, HorizontalGroup, OnValueChanged(nameof(TrySetDefaultCategory))] GameObject _prefab;
        [ShowInInspector, HorizontalGroup, ValueDropdown(nameof(CategoryNames))] string _category;
        [ShowInInspector, HorizontalGroup, Button, EnableIf(nameof(IsAddValidData))]
        void Add() {
            var categoryIndex = Array.FindIndex(categories, c => c.name == _category);
            ref var category = ref categories[categoryIndex];
            if (!Array.Exists(category.clothes, c => c.Prefab == _prefab)) {
                ref var clothes = ref category.clothes;
                Array.Resize(ref clothes, clothes.Length + 1);
                category.clothes[^1] = new Cloth { Prefab = _prefab };

                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);

                Log.Important?.Info($"Added {_prefab.name} to {category.name}");
            } else {
                Log.Important?.Info($"Prefab {_prefab.name} already present in {category.name}");
            }

            _prefab = null;
            _category = string.Empty;
        }

        string[] CategoryNames => Array.ConvertAll(categories, c => c.name);

        bool IsAddValidData() {
            if (_prefab == null) {
                return false;
            }
            var index = Array.FindIndex(categories, c => c.name == _category);
            if (index == -1) {
                return false;
            }
            return true;
        }

        void TrySetDefaultCategory() {
            var index = FindDefaultCategory(_prefab.name);
            if (index != -1) {
                _category = categories[index].name;
            } else if (_category.IsNullOrWhitespace()) {
                _category = categories[0].name;
            }
        }
    }
}
