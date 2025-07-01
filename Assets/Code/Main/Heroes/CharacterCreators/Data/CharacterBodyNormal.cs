using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Data {
    [Serializable]
    public struct CharacterBodyNormal {
        [LocStringCategory(Category.CharacterCreator)]
        public LocString label;
        [UIAssetReference, HideLabel] public ARAssetReference bodyNormal;
        [HideLabel, PropertyRange(0, 1)] public float normalStrength;
    }
}