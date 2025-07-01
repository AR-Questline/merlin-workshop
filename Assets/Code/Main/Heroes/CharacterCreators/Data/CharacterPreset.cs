using System;
using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Data {
    [Serializable]
    public struct CharacterPreset {
        [SerializeField, UIAssetReference(AddressableLabels.UI.CharacterCreator)] 
        ShareableSpriteReference icon;

        public CharacterPresetData data;

        public ShareableSpriteReference Icon => icon;
    }
}