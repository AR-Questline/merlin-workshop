using System;
using System.Collections.Generic;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.SocialServices {
    public enum DlcCategory : byte {
        SupporterPack = 0,
        Sarras = 1,
        ContentPack = 2,
        /// <description>
        /// Use <see cref="DlcCategoryFlags.HorseArmor"/> for checking all possible horse armor DLCs
        /// </description>
        HorseArmorPack = 3
    }

    [Flags]
    public enum DlcCategoryFlags : byte {
        None = 0,
        SupporterPack = 1 << 0,
        Sarras = 1 << 1,
        ContentPack = 1 << 2,
        /// <description>
        /// Use <see cref="HorseArmor"/> for checking all possible horse armor DLCs
        /// </description>
        HorseArmorPack = 1 << 3,
        HorseArmor = HorseArmorPack | SupporterPack,
        AllAllowingDlcDisabling = SupporterPack | ContentPack | HorseArmorPack,
        
        All = byte.MaxValue
    }
    
    public static class DlcCategoryExtensions {
        public static DlcCategoryFlags ToFlags(this DlcCategory category) {
            return category switch {
                DlcCategory.SupporterPack => DlcCategoryFlags.SupporterPack,
                DlcCategory.Sarras => DlcCategoryFlags.Sarras,
                DlcCategory.ContentPack => DlcCategoryFlags.ContentPack,
                DlcCategory.HorseArmorPack => DlcCategoryFlags.HorseArmorPack,
                _ => DlcCategoryFlags.None
            };
        }

        public static DlcCategoryFlags AllCurrentlyActive() {
            var flags = DlcCategoryFlags.None;
            var socialService = SocialService.Get;
            foreach (DlcCategory category in Enum.GetValues(typeof(DlcCategory))) {
                if (socialService.HasDlc(category)) {
                    flags |= category.ToFlags();
                }
            }
            return flags;
        }
        
        public static bool HasAllRequiredDLCs(DlcCategoryFlags dlcs) {
            var dlcsRequired = dlcs & ~DlcCategoryFlags.AllAllowingDlcDisabling;
            if (dlcsRequired == DlcCategoryFlags.None) {
                return true;
            }
            return SocialService.Get.HasDlc(dlcsRequired);
        }
    }
    
    [Serializable]
    public struct DlcId {
        [ToggleLeft]
        public bool isFree;
        
#if UNITY_PS5 || UNITY_EDITOR
        [LabelText("PS5 Id")]
        public Optional<string> ps5Id;
#endif

#if UNITY_GAMECORE || MICROSOFT_GAME_CORE || UNITY_EDITOR
        public Optional<string> xboxStoreId;
#endif

#if UNITY_STANDALONE || UNITY_EDITOR
        public Optional<uint> steamId;
        public Optional<ulong> gogId;
#endif

        // === Category Mapping
        public static DlcId? GetDlcId(DlcCategory category) {
            var commonRefs = Scenes.SceneConstructors.CommonReferences.Get;
            if (commonRefs == null) {
                return null;
            }

            return category switch {
                DlcCategory.SupporterPack => commonRefs.SupportersPackDlcId,
                DlcCategory.Sarras => commonRefs.SarrasDlcId,
                DlcCategory.ContentPack => commonRefs.ContentPackDlcId,
                DlcCategory.HorseArmorPack => commonRefs.HorseDlcId,
                _ => null
            };
        }
        
        public static List<DlcId?> GetDlcId(DlcCategoryFlags categories) {
            var dlcIds = new List<DlcId?>(3);
            foreach (DlcCategory category in Enum.GetValues(typeof(DlcCategory))) {
                if (categories.HasFlagFast(category.ToFlags())) {
                    DlcId? dlcId = GetDlcId(category);
                    if (dlcId.HasValue) {
                        dlcIds.Add(dlcId);
                    }
                }
            }
            return dlcIds;
        }

        // === Helpers
        [Serializable, InlineProperty]
        public struct Optional<T> {
            [HorizontalGroup(Width = 0.1f), SerializeField, HideLabel]
            bool hasValue;

            [HorizontalGroup(Width = 0.89f), SerializeField, HideLabel, ShowIf(nameof(hasValue))]
            T value;

            public bool HasValue => hasValue;

            public T Value => value;

            public bool TryGet(out T value) {
                value = this.value;
                return hasValue;
            }
        }
    }
}