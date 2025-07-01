using Awaken.TG.Assets;
using UnityEngine;
using UnityEngine.Video;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Helpers {
    /// <summary>
    /// Container for all things that don't work inside TextPart
    /// </summary>
    public class SmartTextHelper : MonoBehaviour {
        [ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true, AddressableGroup.Tutorial)]
        public SpriteReference spriteReference;
        [ARAssetReferenceSettings(new []{typeof(VideoClip)}, true, AddressableGroup.Tutorial)]
        public ARAssetReference clipReference;
        
        public bool HasImage => spriteReference != null && spriteReference.arSpriteReference.IsSet;
        public bool HasVideo => clipReference != null && clipReference.IsSet;
    }
}