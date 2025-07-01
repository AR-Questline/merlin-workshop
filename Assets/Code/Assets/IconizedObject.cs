using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.TG.Assets {
    public class IconizedObject : MonoBehaviour, IIconized {
        [IconRenderingSettings, ARAssetReferenceSettings(new[] { typeof(Texture2D), typeof(Sprite) }, true, AddressableGroup.ItemsIcons)] [SerializeField]
        ShareableSpriteReference iconReference;
        
        ShareableSpriteReference IIconized.GetIconReference() => iconReference;
        
        public ShareableSpriteReference IconReference => iconReference;
        
        void  IIconized.SetIconReference(ShareableSpriteReference iconRef) => this.iconReference = iconRef;
        
        GameObject IIconized.InstantiateProp(Transform parent) {
#if UNITY_EDITOR
            return UnityEditor.PrefabUtility.InstantiatePrefab(gameObject, parent) as GameObject;
#else
            return null;
#endif
        }
    }
}