using System;
using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Controllers {
    [Serializable]
    public struct TemporaryMaterialTextureLoader<T> where T : Texture {
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(Texture) })]
        ARAssetReference assetRef;
        public bool HasAssetRef => assetRef.IsSet;
        
        Func<bool> _shouldBeLoadedCheck;
        Func<bool> _shouldBeUnloadedCheck;

        Material _material;
        int _propertyId;
        Action<ARAsyncOperationHandle<T>> _onTextureLoaded;
        ARAsyncOperationHandle<T> _handle;

        public void Init(Func<bool> shouldBeLoadedCheck, Func<bool> shouldBeUnloadedCheck, Material material, int propertyId) {
            _shouldBeLoadedCheck = shouldBeLoadedCheck;
            _shouldBeUnloadedCheck = shouldBeUnloadedCheck;
            _material = material;
            _propertyId = propertyId;
            _onTextureLoaded = OnTextureLoaded;
        }

        public void Update() {
            if (_shouldBeLoadedCheck()) {
                if (_handle.Equals(default)) {
                    LoadTextureAsync();
                }
                return;
            }

            if (_handle.Equals(default) == false && _shouldBeUnloadedCheck()) {
                UnloadTexture();
            }
        }
        
        public void LoadTextureAsync() {
            _handle = assetRef.LoadAsset<T>();
            _handle.OnComplete(_onTextureLoaded);
        }

        public void UnloadTexture() {
            _handle.Release();
            _handle = default;
            if (_material != null) {
               _material.SetTexture(_propertyId, null);
            }
        }

        void OnTextureLoaded(ARAsyncOperationHandle<T> texture) {
            _material.SetTexture(_propertyId, texture.Result);
        }
    }
}