using System;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Assets {
    /// <summary>
    /// Allows to safe share <see cref="SpriteReference"/>
    /// </summary>
    [Serializable]
    public sealed partial class ShareableSpriteReference {
        public ushort TypeForSerialization => SavedTypes.ShareableSpriteReference;

        // === Properties
        /// <summary>
        /// Check if is valid sprite reference
        /// </summary>
        [JsonIgnore] public bool IsSet => !string.IsNullOrWhiteSpace(arSpriteReference?.Address);
        [JsonIgnore] [UnityEngine.Scripting.Preserve] public string AssetGUID => arSpriteReference?.Address;
        
        // === Editor references
        [Saved] public ARAssetReference arSpriteReference;
        
        public ShareableSpriteReference() { }

        public ShareableSpriteReference(string guid, string subObject = "") {
            arSpriteReference = new ARAssetReference(guid, subObject);
        }

        public ShareableSpriteReference(SpriteReference spriteReference) {
            arSpriteReference = spriteReference.arSpriteReference.DeepCopy();
        }

        // === Operations
        /// <summary>
        /// Obtain <see cref="SpriteReference"/> to singular usage
        /// </summary>
        public SpriteReference Get() {
            return new SpriteReference() {
                arSpriteReference = arSpriteReference.DeepCopy(),
            };
        }

        /// <summary>
        /// Register sprite to auto release and set sprite to image
        /// This is async operation, image will be set after sprite get loaded
        /// </summary>
        /// <param name="owner">View which uses sprite</param>
        /// <param name="image">Sprite target</param>
        /// <param name="afterAssign">Optional callback after sprite loaded and assigned</param>
        public void RegisterAndSetup(IReleasableOwner owner, Image image, Action<Image, Sprite> afterAssign = null) {
            var reference = Get();
            reference.RegisterAndSetup(owner, image, afterAssign);
        }
        
        public void RegisterAndSetup(IReleasableOwner owner, VisualElement image, Action<VisualElement, Sprite> afterAssign = null) {
            var reference = Get();
            reference.RegisterAndSetup(owner, image, afterAssign);
        }
        
        public void TryRegisterAndSetup(IReleasableOwner owner, Image image, Action<Image, Sprite> afterAssign = null) {
            if (IsSet) {
                RegisterAndSetup(owner, image, afterAssign);
            } else {
                Log.Important?.Warning("Cannot register and setup empty sprite reference", image);
            }
        }

        public ShareableSpriteReference DeepCopy() {
            return new ShareableSpriteReference(arSpriteReference.Address, arSpriteReference.SubObjectName);
        }
        
        // === Casting
        public static implicit operator ShareableSpriteReference(ShareableARAssetReference shareableARAsset) {
            return new ShareableSpriteReference(shareableARAsset.AssetGUID, shareableARAsset.SubObject);
        }
    }
}