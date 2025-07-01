using System;
using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Data {
    [Serializable]
    public struct CharacterCloth {
        [SerializeField, UIAssetReference(AddressableLabels.UI.CharacterCreator)] 
        ShareableSpriteReference icon;
        
        [ARAssetReferenceSettings(new[] {typeof(GameObject)})]
        [SerializeField] ARAssetReference asset;

        public ShareableSpriteReference Icon => icon;
        public ARAssetReference Asset => asset;

        public CharacterCloth(ShareableSpriteReference icon, ARAssetReference asset) {
            this.icon = icon;
            this.asset = asset;
        }
    }
    
    [Serializable]
    public struct CharacterTexture {
        [SerializeField, UIAssetReference(AddressableLabels.UI.CharacterCreator)] 
        ShareableSpriteReference icon;
        
        [SerializeField, TextureAssetReference] ShareableARAssetReference asset;

        public ShareableSpriteReference Icon => icon;
        public ARAssetReference Asset => asset.Get();

        public CharacterTexture(ShareableSpriteReference icon, ShareableARAssetReference asset) {
            this.icon = icon;
            this.asset = asset;
        }
    }
}