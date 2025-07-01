using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Searching {
    public abstract class AllowedReadableAssetsWindow<TAssetType, TAssetImporter> : OdinEditorWindow
        where TAssetType : UnityEngine.Object
        where TAssetImporter : AssetImporter {
        const string FoundReadableAssetTabName = "Found readable assets";
        const string AllowedReadableAssetTabName = "Allowed readable assets";

        [Searchable, TabGroup(FoundReadableAssetTabName)]
        public List<AssetWithStates> readableAssetsWithStates;

        [Searchable, TabGroup(AllowedReadableAssetTabName), OnValueChanged(nameof(OnAllowedReadableAssetsChanged))]
        public List<TAssetType> allowedReadableAssets;

        protected override void Initialize() {
            base.Initialize();
            RefreshReadableAssets();
            RefreshAllowedReadableAssets();
        }

        [Button, TabGroup(FoundReadableAssetTabName)]
        void ApplyAll() {
            var allowedReadableAssetsSO = GetAllowedReadableAssetsSO();
            var newNotReadableAssets = new List<TAssetType>();
            int readableAssetsCount = readableAssetsWithStates.Count;
            bool changedAllowedReadableAsssetsSO = false;
            for (int i = readableAssetsCount - 1; i >= 0; i--) {
                var assetWithStates = readableAssetsWithStates[i];
                if (assetWithStates.isAllowedReadable | (assetWithStates.isReadable == false)) {
                    if (assetWithStates.isAllowedReadable) {
                        changedAllowedReadableAsssetsSO |= allowedReadableAssetsSO.values.AddUnique(assetWithStates.asset);
                    }

                    if (assetWithStates.isReadable == false) {
                        newNotReadableAssets.Add(assetWithStates.asset);
                    }

                    Unity.Collections.ListExtensions.RemoveAtSwapBack(readableAssetsWithStates, i);
                }
            }

            if (changedAllowedReadableAsssetsSO) {
                EditorUtility.SetDirty(allowedReadableAssetsSO);
            }

            AssetDatabase.StartAssetEditing();
            try {
                foreach (var newNotReadableAsset in newNotReadableAssets) {
                    SetAssetNotReadableAndReimport(newNotReadableAsset);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                AssetDatabase.StopAssetEditing();
            }
        }

        [Button, TabGroup(FoundReadableAssetTabName)]
        void MakeNotReadableAll() {
            for (int i = 0; i < readableAssetsWithStates.Count; i++) {
                var assetWithState = readableAssetsWithStates[i];
                assetWithState.isReadable = false;
                readableAssetsWithStates[i] = assetWithState;
            }
        }

        [Button, TabGroup(FoundReadableAssetTabName)]
        void RefreshReadableAssets() {
            var readableAssets = new List<TAssetType>(64);
            FindAllAssetsWithReadWriteEnabled(readableAssets);
            var allowedReadableAssetsSO = GetAllowedReadableAssetsSO();
            int readableAssetsCount = readableAssets.Count;
            for (int i = readableAssetsCount - 1; i >= 0; i--) {
                if (allowedReadableAssetsSO.values.Contains(readableAssets[i])) {
                    readableAssets.RemoveAtSwapBack(i);
                }
            }

            readableAssetsWithStates = new List<AssetWithStates>(readableAssets.Count);
            foreach (var asset in readableAssets) {
                readableAssetsWithStates.Add(new(this, asset));
            }
        }

        [Button, TabGroup(AllowedReadableAssetTabName)]
        void RefreshAllowedReadableAssets() {
            allowedReadableAssets = new List<TAssetType>(GetAllowedReadableAssetsSO().values);
        }

        public void SetAssetNotReadableAndReimport(TAssetType asset) {
            var assetImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(asset)) as TAssetImporter;
            if (assetImporter == null) {
                Log.Important?.Error($"Asset imported is null for asset {asset?.name}", asset);
                return;
            }

            SetIsReadable(assetImporter, false);
        }

        public void SetIsAllowedReadable(AssetWithStates assetWithStates) {
            var allowedReadableAssetsSO = GetAllowedReadableAssetsSO();
            bool changed = allowedReadableAssetsSO.values.AddUnique(assetWithStates.asset);
            if (changed) {
                EditorUtility.SetDirty(allowedReadableAssetsSO);
            }
        }

        public void RemoveFromReadableList(AssetWithStates assetWithStates) {
            readableAssetsWithStates.Remove(assetWithStates);
        }

        protected abstract string GetAssetName();
        protected abstract AllowedReadableAssetsSingleton<TAssetType> GetAllowedReadableAssetsSO();
        protected abstract bool IsReadable(TAssetImporter assetImporter);
        protected abstract void SetIsReadable(TAssetImporter assetImporter, bool isReadable);

        void OnAllowedReadableAssetsChanged() {
            var allowedReadableAssetsSO = GetAllowedReadableAssetsSO();
            allowedReadableAssetsSO.values = new List<TAssetType>(allowedReadableAssets);
            EditorUtility.SetDirty(allowedReadableAssetsSO);
            for (int i = readableAssetsWithStates.Count - 1; i >= 0; i--) {
                if (allowedReadableAssets.Contains(readableAssetsWithStates[i].asset)) {
                    readableAssetsWithStates.RemoveAtSwapBack(i);
                }
            }
        }

        void FindAllAssetsWithReadWriteEnabled(List<TAssetType> readableAssets) {
            List<string> paths = new(64);
            AssetDatabaseUtils.GetAssetsPaths(GetAssetName(), paths);
            foreach (var path in paths) {
                TAssetImporter importer = AssetImporter.GetAtPath(path) as TAssetImporter;
                if (importer == null) {
                    continue;
                }

                if (IsReadable(importer)) {
                    var asset = AssetDatabase.LoadAssetAtPath<TAssetType>(path);
                    if (asset == null) {
                        Log.Important?.Error($"Failed to load asset at path {path}");
                        continue;
                    }

                    readableAssets.Add(asset);
                }
            }
        }

        public struct AssetWithStates : IEquatable<AssetWithStates> {
            const string HorizontalGroupInfoName = "Info";
            const string HorizontalGroupButtonsName = "Buttons";

            [HorizontalGroup(HorizontalGroupInfoName)]
            [VerticalGroup(HorizontalGroupInfoName + "/Info"), InlineProperty]
            public TAssetType asset;

            [VerticalGroup(HorizontalGroupInfoName + "/States"), InlineProperty]
            public bool isAllowedReadable;

            [VerticalGroup(HorizontalGroupInfoName + "/States"), InlineProperty]
            public bool isReadable;

            AllowedReadableAssetsWindow<TAssetType, TAssetImporter> _window;

            public AssetWithStates(AllowedReadableAssetsWindow<TAssetType, TAssetImporter> window, TAssetType asset) {
                this.asset = asset;
                _window = window;
                isAllowedReadable = false;
                isReadable = true;
            }

            [HorizontalGroup(HorizontalGroupButtonsName), Button]
            void Apply() {
                if (isAllowedReadable) {
                    _window.SetIsAllowedReadable(this);
                }

                if (isReadable == false) {
                    _window.SetAssetNotReadableAndReimport(asset);
                }

                if (isAllowedReadable | (isReadable == false)) {
                    _window.RemoveFromReadableList(this);
                }
            }

            public bool Equals(AssetWithStates other) {
                return EqualityComparer<TAssetType>.Default.Equals(asset, other.asset);
            }

            public override bool Equals(object obj) {
                return obj is AssetWithStates other && Equals(other);
            }

            public override int GetHashCode() {
                return EqualityComparer<TAssetType>.Default.GetHashCode(asset);
            }
        }
    }
}