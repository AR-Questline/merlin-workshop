using System;
using Awaken.TG.Assets;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Data {
    [Serializable]
    public struct CharacterSkinConfig {
        [SerializeField, TextureAssetReference, CanBeNull] ShareableARAssetReference albedo;
        [SerializeField, TextureAssetReference, CanBeNull] ShareableARAssetReference normal;
        [SerializeField, TextureAssetReference, CanBeNull] ShareableARAssetReference mask;

        public ShareableSpriteReference Icon { get; }
        public ShareableARAssetReference Albedo => albedo;
        public ShareableARAssetReference Normal => normal;
        public ShareableARAssetReference Mask => mask;
        
        public CharacterSkinConfig(ShareableSpriteReference icon, CharacterSkinConfig skinConfig) {
            Icon = icon;
            this.albedo = skinConfig.Albedo;
            this.normal = skinConfig.Normal;
            this.mask = skinConfig.Mask;
        }
    }
}