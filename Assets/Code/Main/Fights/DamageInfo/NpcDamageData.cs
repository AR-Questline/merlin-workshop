using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.DamageInfo {
    [Serializable]
    public struct NpcDamageData {
        [SerializeField] DamageSource damageSource;
        [SerializeField, ShowIf(nameof(UsesNpcDamage))] NpcDamageValueType damageValueType;
        [SerializeField, ShowIf(nameof(UsesMultiplier)), RichEnumExtends(typeof(EquipmentSlotType))] RichEnumReference statsFromSlot;
        [SerializeField, ShowIf(nameof(UsesMultiplier))] bool useItemDamageMultiplier;
        [SerializeField, ShowIf(nameof(UsesMultiplier))] float additionalMultiplier;
        [SerializeField, ShowIf(nameof(UsesCustomDamage))] float customDamage;
        [SerializeField, BoxGroup("DamageType")] DamageType damageType;
        [SerializeField, BoxGroup("DamageType")] List<DamageTypeDataConfig> damageSubTypes;
        bool UsesMultiplier => damageSource is DamageSource.EquippedItem or DamageSource.NpcDamage;
        bool UsesNpcDamage => damageSource is DamageSource.NpcDamage;
        bool UsesCustomDamage => damageSource is DamageSource.Custom;
        
        public static NpcDamageData DefaultAttackData => new NpcDamageData {
            damageSource = DamageSource.EquippedItem,
            statsFromSlot = EquipmentSlotType.MainHand,
            damageType = DamageType.PhysicalHitSource,
            additionalMultiplier = 1f,
        };
        
        public static NpcDamageData DefaultRangedAttackData => new NpcDamageData {
            damageSource = DamageSource.NpcDamage,
            statsFromSlot = EquipmentSlotType.MainHand,
            damageType = DamageType.PhysicalHitSource,
            damageValueType = NpcDamageValueType.Ranged,
            additionalMultiplier = 1f,
        };
        
        public static NpcDamageData DefaultMagicAttackData => new NpcDamageData {
            damageSource = DamageSource.NpcDamage,
            statsFromSlot = EquipmentSlotType.MainHand,
            damageType = DamageType.MagicalHitSource,
            damageValueType = NpcDamageValueType.Magic,
            additionalMultiplier = 1f,
        };

        public RawDamageData GetRawDamageData(NpcElement npc, float baseDamageMultiplier = 1f) {
            float damageValue;
            float multiplier;
            
            switch (damageSource) {
                case DamageSource.EquippedItem:
                    var item = GetItem(npc);
                    damageValue = GetDamageBasedOnEquippedItem(npc, item);
                    multiplier = useItemDamageMultiplier ? GetItemDamageMultiplier(item) : 1f;
                    multiplier *= additionalMultiplier;
                    break;
                case DamageSource.NpcDamage:
                    damageValue = damageValueType switch {
                        NpcDamageValueType.Melee => npc.Stat(NpcStatType.MeleeDamage),
                        NpcDamageValueType.Ranged => npc.Stat(NpcStatType.RangedDamage),
                        NpcDamageValueType.Magic => npc.Stat(NpcStatType.MagicDamage),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    multiplier = useItemDamageMultiplier ? GetItemDamageMultiplier(GetItem(npc)) : 1f;
                    multiplier *= additionalMultiplier;
                    break;
                case DamageSource.Custom:
                    return new RawDamageData(customDamage);
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return new RawDamageData(damageValue * baseDamageMultiplier, multiplier, 0);
        }
        
        public DamageTypeData GetDamageTypeData(NpcElement npc) {
            if (damageSource is DamageSource.EquippedItem) {
                return GetItem(npc)?.ItemStats?.DamageTypeData;
            }
            
            if (damageSubTypes is not { Count: > 0 }) {
                return new DamageTypeData(damageType);
            }
            
            var subTypes = new DamageTypeDataPart[damageSubTypes.Count];
            for (int i = 0; i < damageSubTypes.Count; i++) {
                subTypes[i] = DamageTypeDataConfig.Construct(damageSubTypes[i], damageType);
            }
            return new DamageTypeData(damageType, subTypes.ToList());
        }

        Item GetItem(NpcElement npc) {
            return npc.Inventory.EquippedItem(statsFromSlot.EnumAs<EquipmentSlotType>());
        }

        float GetDamageBasedOnEquippedItem(NpcElement npc, Item item) {
            Damage.GetDmgSourceDamage(npc, item?.ItemStats, out float damage);
            float multiplier = useItemDamageMultiplier ? GetItemDamageMultiplier(item) : 1f;
            return damage * multiplier;
        }

        float GetItemDamageMultiplier(Item item) {
            return item?.Stat(ItemStatType.NpcDamageMultiplier) ?? 1f;
        }

        public enum DamageSource : byte {
            EquippedItem,
            NpcDamage,
            Custom,
        }

        public enum NpcDamageValueType : byte {
            Melee,
            Ranged,
            Magic,
        }
    }
}
