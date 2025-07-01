using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Statuses {
    [Serializable]
    public struct StatusConsumeData {
        [SerializeField, RichEnumExtends(typeof(BuildupStatusType))]
        public RichEnumReference statusType;

        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        public ShareableARAssetReference vfx;
        
        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        public ShareableARAssetReference handVfx;
        
        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        public ShareableARAssetReference explosionVfx;
        
        public StatusConsumeType consumeType;

        public BuildupStatusType StatusType => statusType.EnumAs<BuildupStatusType>();
    }
}