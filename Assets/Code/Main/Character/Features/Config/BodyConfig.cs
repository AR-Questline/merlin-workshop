using System;
using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features.Config {
    [Serializable]
    public struct BodyConfig { 
        [TextureAssetReference] public ShareableARAssetReference albedo;
        [TextureAssetReference] public ShareableARAssetReference mask;
        [TextureAssetReference] public ShareableARAssetReference normal;
        [Space]
        [TextureAssetReference] public ShareableARAssetReference bodyAlbedo;
        [TextureAssetReference] public ShareableARAssetReference bodyMask;
        [TextureAssetReference] public ShareableARAssetReference bodyNormal;
        
        public bool Invalid => albedo is not {IsSet: true} && 
                               mask is not {IsSet: true} && 
                               normal is not {IsSet: true} && 
                               bodyAlbedo is not {IsSet: true} && 
                               bodyMask is not {IsSet: true} && 
                               bodyNormal is not {IsSet: true};
    }
}
