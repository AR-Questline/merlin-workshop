using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Awaken.TG.Assets {
    /// <summary>
    /// Wrapper to lazy load from sprite atlas
    /// Hides asynchronous operation via exposing synchronous API
    /// </summary>
    [Serializable]
    public class SpriteAtlasReference : IReleasableReference {
        // === Editor references
        public ARAssetReference arSpriteReference;

        // === Operations
        /// <summary>
        /// Set sprite in image after sprite atlas get loaded
        /// </summary>
        public void SetSprite(Image image, string spriteName, Action<Image, Sprite> afterAssign = null) {
            arSpriteReference.LoadAsset<SpriteAtlas>().OnComplete(handle => AssignSprite(image, spriteName, handle, afterAssign));
        }

        /// <summary>
        /// Release sprite asset
        /// </summary>
        public void Release() {
            arSpriteReference.ReleaseAsset();
        }
        
        void AssignSprite(Image image,string spriteName, ARAsyncOperationHandle<SpriteAtlas> handle, Action<Image, Sprite> afterAssign) {
            if (image != null) {
                var sprite = handle.Result.GetSprite(spriteName);
                image.sprite = sprite;
                afterAssign?.Invoke(image, sprite);
            } else {
                Release();
            }
        }
    }
}