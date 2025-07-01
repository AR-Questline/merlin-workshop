using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    [Serializable]
    public struct CrimeArchetype {
        [SerializeField] SimpleCrimeType type;
        [SerializeField, ShowIf(nameof(ShowItemValue))] CrimeItemValue itemValue;
        [SerializeField, ShowIf(nameof(ShowNpcValue))] CrimeNpcValue npcValue;

        bool ShowItemValue => type is SimpleCrimeType.Theft or SimpleCrimeType.Pickpocket;
        bool ShowNpcValue => type is SimpleCrimeType.Pickpocket or SimpleCrimeType.Combat or SimpleCrimeType.Murder;
        
        public readonly CrimeItemValue ItemValue => itemValue;
        public readonly CrimeNpcValue NpcValue => npcValue;

        public readonly SimpleCrimeType SimpleCrimeType => type;
        public readonly CrimeType CrimeType => type.ToCrimeType();

        public readonly bool IsNoCrime => type switch {
            SimpleCrimeType.None => true,
            SimpleCrimeType.Trespassing => false,
            SimpleCrimeType.Theft => itemValue == CrimeItemValue.None,
            SimpleCrimeType.Pickpocket => npcValue == CrimeNpcValue.None,
            SimpleCrimeType.Combat => false,
            SimpleCrimeType.Murder => npcValue == CrimeNpcValue.None,
            SimpleCrimeType.Lockpicking => false,
            SimpleCrimeType.Custom => false,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        public readonly float Bounty(CrimeOwnerTemplate crimeOwner) => type switch {
            SimpleCrimeType.None => 0,
            SimpleCrimeType.Trespassing => crimeOwner.TrespassingBounty,
            SimpleCrimeType.Theft => crimeOwner.ItemBounty(itemValue).theft,
            SimpleCrimeType.Pickpocket => crimeOwner.ItemBounty(itemValue).theft * crimeOwner.NpcBounty(npcValue).pickpocketMultiplier,
            SimpleCrimeType.Combat => crimeOwner.CombatBounty,
            SimpleCrimeType.Murder => crimeOwner.NpcBounty(npcValue).murder,
            SimpleCrimeType.Lockpicking => crimeOwner.LockpickingBounty,
            SimpleCrimeType.Custom => 1,
            _ => throw new ArgumentOutOfRangeException()
        };

        CrimeArchetype(SimpleCrimeType type, CrimeItemValue itemValue = CrimeItemValue.None, CrimeNpcValue npcValue = CrimeNpcValue.None) {
            this.type = type;
            this.itemValue = itemValue;
            this.npcValue = npcValue;
        }
        CrimeArchetype(SimpleCrimeType type, CrimeNpcValue npcValue) : this(type, CrimeItemValue.None, npcValue) { }

        public static readonly CrimeArchetype None = new(SimpleCrimeType.None);
        public static readonly CrimeArchetype Trespassing = new(SimpleCrimeType.Trespassing);
        public static readonly CrimeArchetype Lockpicking = new(SimpleCrimeType.Lockpicking);
        public static readonly CrimeArchetype Custom = new(SimpleCrimeType.Custom);
        
        public static CrimeArchetype Theft(CrimeItemValue itemValue) => new(SimpleCrimeType.Theft, itemValue);
        public static CrimeArchetype Theft(Item item) => new(SimpleCrimeType.Theft, ItemCrimeValue(item));

        public static CrimeArchetype Pickpocketing(CrimeItemValue itemValue, CrimeNpcValue npcValue) => new(SimpleCrimeType.Pickpocket, itemValue, npcValue);
        public static CrimeArchetype Pickpocketing(Item item, CrimeNpcValue npcValue) => new(SimpleCrimeType.Pickpocket, ItemCrimeValue(item), npcValue);
        
        public static CrimeArchetype Combat(CrimeNpcValue npcValue) => new(SimpleCrimeType.Combat, npcValue);
        public static CrimeArchetype Murder(CrimeNpcValue npcValue) => new(SimpleCrimeType.Murder, npcValue);
        
        static CrimeItemValue ItemCrimeValue(Item item) => ICrimeDisabler.IsCrimeDisabled(item, CrimeArchetype.Theft(item.CrimeValue)) ? CrimeItemValue.None : item.CrimeValue;

        public readonly override bool Equals(object obj) {
            return obj is CrimeArchetype other && Equals(other);
        }
        
        public readonly bool Equals(CrimeArchetype other) => type == other.type && type switch {
            SimpleCrimeType.None => true,
            SimpleCrimeType.Trespassing => true,
            SimpleCrimeType.Theft => itemValue == other.itemValue,
            SimpleCrimeType.Pickpocket => itemValue == other.itemValue && npcValue == other.npcValue,
            SimpleCrimeType.Combat => npcValue == other.NpcValue,
            SimpleCrimeType.Murder => npcValue == other.npcValue,
            SimpleCrimeType.Lockpicking => true,
            SimpleCrimeType.Custom => true,
            _ => throw new ArgumentOutOfRangeException()
        };

        public readonly override int GetHashCode() => type switch {
            SimpleCrimeType.None => 0,
            SimpleCrimeType.Trespassing => 1,
            SimpleCrimeType.Theft => DHash.Combine(2, (int) itemValue),
            SimpleCrimeType.Pickpocket => DHash.Combine(2, (int) itemValue, (int) npcValue),
            SimpleCrimeType.Combat => DHash.Combine(2, (int) npcValue),
            SimpleCrimeType.Murder => DHash.Combine(2, (int) npcValue),
            SimpleCrimeType.Lockpicking => 6,
            SimpleCrimeType.Custom => 7,
            _ => throw new ArgumentOutOfRangeException()
        };

        public static bool operator ==(in CrimeArchetype left, in CrimeArchetype right) {
            return left.Equals(right);
        }

        public static bool operator !=(in CrimeArchetype left, in CrimeArchetype right) {
            return !left.Equals(right);
        }
    }
}