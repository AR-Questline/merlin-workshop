using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.PatchNotes {
    [Serializable]
    public class PatchNote {
        [FoldoutGroup("@version")]
        public string version;
        [Tooltip("If true, will show even if a suffix is added to the version")]
        public bool majorVersionNotes;
        [FoldoutGroup("@version"), LocStringCategory(Category.UI)]
        public LocString title;
        [FoldoutGroup("@version"), LocStringCategory(Category.UI)]
        public LocString message;
        [ARAssetReferenceSettings(new [] {typeof(Sprite), typeof(Texture)}, group: AddressableGroup.Stories)]
        public ShareableSpriteReference artRef;
    }
}