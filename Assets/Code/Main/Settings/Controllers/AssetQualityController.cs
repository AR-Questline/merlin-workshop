using Awaken.TG.Assets;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Settings.Controllers {
    public abstract class AssetQualityController<T> : StartDependentView<GeneralGraphics>, IGeneralGraphicsSceneView where T : class {
        ARAssetReference _activeAssetRef;
        ListDictionary<ARAssetReference, ARAsyncOperationHandle<T>> _assetRefsToHandles;
        protected abstract bool IsNullAssetValidOption { get; }
        protected abstract int QualityLevelsCount { get; }
        protected abstract bool DontSetIfNotInitiallySet { get; }
        protected abstract bool IsAnyQualityAssetSet { get; }

        protected abstract bool EnsureValidQualityOptionsTarget();
        protected abstract void SetAsset(T asset);
        protected abstract ARAssetReference GetAssetForQuality(int qualityIndex);
        protected abstract ARAssetReference GetFallbackAsset();

        protected override void OnInitialize() {
            _assetRefsToHandles = new(QualityLevelsCount);
            OnSettingChanged(Target);
        }

        public void SettingsRefreshed(GeneralGraphics graphicsSetting) {
            OnSettingChanged(graphicsSetting);
        }

        void OnSettingChanged(GeneralGraphics graphicsSetting) {
            var settingIndex = graphicsSetting.ActiveIndex;
            LoadAssetForGraphicsOption(settingIndex);
        }

        void LoadAssetForGraphicsOption(int settingIndex) {
            if (!EnsureValidQualityOptionsTarget()) {
                Log.Debug?.Warning($"Quality options target is not loaded yet");
                return;
            }

            var assetRef = (DontSetIfNotInitiallySet && !IsAnyQualityAssetSet) ? null : (GetAssetForQuality(settingIndex) ?? GetFallbackAsset());
            if (assetRef == null || !assetRef.IsSet) {
                if (!IsNullAssetValidOption) {
                    Log.Debug?.Warning(
                        $"No asset provided in {nameof(AssetQualityController<T>)} for {typeof(T).Name} in {this.gameObject.name}. Will be set to none");
                }

                SetAsset(null);
                ReleaseAllLoadedAssets();
                return;
            }

            if (assetRef.Equals(_activeAssetRef)) {
                return;
            }

            if (HasBeenDiscarded) {
                return;
            }

            _activeAssetRef = assetRef;
            if (_assetRefsToHandles.ContainsKey(assetRef)) {
                return;
            }

            var loadAssetOpHandle = assetRef.LoadAsset<T>();
            _assetRefsToHandles.Add(assetRef, loadAssetOpHandle);
            loadAssetOpHandle.OnComplete(ProcessLoadedAsset, ProcessLoadedAsset);
        }

        void ProcessLoadedAsset(ARAsyncOperationHandle<T> loadedAssetOpHandle) {
            if (HasBeenDiscarded) {
                return;
            }

            int handleIndexInDict = _assetRefsToHandles.IndexOfValue(loadedAssetOpHandle);
            if (handleIndexInDict == -1) {
                Log.Important?.Error(
                    "Loaded handle was not registered in dictionary. Check if you are adding handle to dictionary after receiving a handle");
                return;
            }

            if (loadedAssetOpHandle.IsDone) {
                var loadedAssetRef = _assetRefsToHandles.GetKeyAtIndex(handleIndexInDict);
                if (loadedAssetRef.Equals(_activeAssetRef)) {
                    //If loaded asset is correct - set asset
                    if (loadedAssetOpHandle.Result != null) {
                        SetAsset(loadedAssetOpHandle.Result);
                        //Release all other loaded assets
                        ReleaseLoadedAllAssetsExceptOne(loadedAssetOpHandle);
                    } else {
                        //Loading went wrong
                        Log.Important?.Error($"Asset loading was finished but {nameof(AsyncOperationHandle)}.Result was null");
                        loadedAssetOpHandle.Release();
                        _assetRefsToHandles.RemoveAt(handleIndexInDict);
                    }
                } else {
                    //If while loading this asset other asset was
                    //set to be the active one - release this loaded asset 
                    loadedAssetOpHandle.Release();
                    _assetRefsToHandles.RemoveAt(handleIndexInDict);
                }
            } else if (loadedAssetOpHandle.IsCancelled) {
                //If while loading this asset loading was cancelled - it was released
                //so just remove from list
                _assetRefsToHandles.RemoveAt(handleIndexInDict);
            } else {
                //Handle is in progress
                Log.Important?.Error("Using handle which is not loaded.");
            }
        }

        void ReleaseAllLoadedAssets() {
            for (int i = 0; i < _assetRefsToHandles.Count; i++) {
                _assetRefsToHandles.GetValueAtIndex(i).Release();
            }
            _assetRefsToHandles.Clear();
        }

        void ReleaseLoadedAllAssetsExceptOne(ARAsyncOperationHandle<T> excludedOpHandle) {
            for (int i = _assetRefsToHandles.Count - 1; i >= 0; i--) {
                var opHandle = _assetRefsToHandles.GetValueAtIndex(i);
                if (!opHandle.Equals(excludedOpHandle)) {
                    excludedOpHandle.Release();
                    _assetRefsToHandles.RemoveAt(i);
                }
            }
        }
        
        protected override IBackgroundTask OnDiscard() {
            ReleaseAllLoadedAssets();
            return base.OnDiscard();
        }
    }
}