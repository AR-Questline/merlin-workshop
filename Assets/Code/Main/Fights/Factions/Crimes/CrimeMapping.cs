using System;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    [Serializable]
    public struct CrimeMapping<T> {
        [SerializeField, ListDrawerSettings] Entry[] entries;

        public CrimeMapping(Entry[] entries) {
            this.entries = entries;
        }
        
        public readonly ref readonly T Get(in CrimeArchetype archetype, in T fallback) {
            for (int i = 0; i < entries.Length; i++) {
                if (entries[i].Match(archetype)) {
                    return ref entries[i].output;
                }
            }
            return ref fallback;
        }
        
        [Serializable]
        public struct Entry {
            [SerializeField, HorizontalGroup("Horizontal"), VerticalGroup("Horizontal/Filter"), HideLabel] Filter filter;
            [SerializeField, VerticalGroup("Horizontal/Filter"), ShowIf(nameof(ShowItemValue))] CrimeItemValue itemValue;
            [SerializeField, VerticalGroup("Horizontal/Filter"), ShowIf(nameof(ShowNpcValue))] CrimeNpcValue npcValue;

            [HideLabel, InlineProperty, HorizontalGroup("Horizontal")] public T output;
            
            readonly bool ShowItemValue => filter is Filter.Theft or Filter.AllTheftsOrPickpocketsByItem or Filter.Pickpocket or Filter.AllPickpocketsByItem;
            readonly bool ShowNpcValue => filter is Filter.Pickpocket or Filter.AllPickpocketsByNpc or Filter.Murder;
            
            readonly CrimeItemValue ItemValue => itemValue;
            readonly CrimeNpcValue NpcValue => npcValue;

            Entry(Filter filter, CrimeItemValue itemValue = CrimeItemValue.None, CrimeNpcValue npcValue = CrimeNpcValue.None) {
                this.filter = filter;
                this.itemValue = itemValue;
                this.npcValue = npcValue;
                output = default;
            }
            public static Entry AllCrimes() => new(Filter.AllCrimes);
            public static Entry AllTrespassing() => new(Filter.AllTrespassing);
            public static Entry AllTheftsOrPickpockets() => new(Filter.AllTheftsOrPickpockets);
            public static Entry AllTheftsOrPickpocketsByItem(CrimeItemValue itemValue) => new(Filter.AllTheftsOrPickpocketsByItem, itemValue);
            public static Entry AllThefts() => new(Filter.AllThefts);
            public static Entry Theft(CrimeItemValue itemValue) => new(Filter.Theft, itemValue);
            public static Entry AllPickpockets() => new(Filter.AllPickpockets);
            public static Entry AllPickpocketsByNpc(CrimeNpcValue npcValue) => new(Filter.AllPickpocketsByNpc, CrimeItemValue.None, npcValue);
            public static Entry AllPickpocketsByItem(CrimeItemValue itemValue) => new(Filter.AllPickpocketsByItem, itemValue);
            public static Entry Pickpocket(CrimeItemValue itemValue, CrimeNpcValue npcValue) => new(Filter.Pickpocket, itemValue, npcValue);
            public static Entry AllCombat() => new(Filter.AllCombat); 
            public static Entry AllMurder() => new(Filter.AllMurder);
            public static Entry Murder(CrimeNpcValue npcValue) => new(Filter.Murder, CrimeItemValue.None, npcValue);

            public Entry WithOutput(in T output) {
                this.output = output;
                return this;
            }
            
            public readonly bool Match(in CrimeArchetype archetype) {
                return filter switch {
                    Filter.AllCrimes => true,
                    Filter.AllTrespassing => archetype.SimpleCrimeType is SimpleCrimeType.Trespassing,
                    Filter.AllTheftsOrPickpockets =>  archetype.SimpleCrimeType is SimpleCrimeType.Theft or SimpleCrimeType.Pickpocket,
                    Filter.AllTheftsOrPickpocketsByItem => archetype.SimpleCrimeType is SimpleCrimeType.Theft or SimpleCrimeType.Pickpocket && archetype.ItemValue == ItemValue,
                    Filter.AllThefts => archetype.SimpleCrimeType is SimpleCrimeType.Theft,
                    Filter.Theft => archetype.Equals(CrimeArchetype.Theft(ItemValue)),
                    Filter.AllPickpockets => archetype.SimpleCrimeType is SimpleCrimeType.Pickpocket,
                    Filter.AllPickpocketsByNpc => archetype.SimpleCrimeType is SimpleCrimeType.Pickpocket && archetype.NpcValue == NpcValue,
                    Filter.AllPickpocketsByItem => archetype.SimpleCrimeType is SimpleCrimeType.Pickpocket && archetype.ItemValue == ItemValue,
                    Filter.Pickpocket => archetype.Equals(CrimeArchetype.Pickpocketing(ItemValue, NpcValue)),
                    Filter.AllCombat => archetype.SimpleCrimeType is SimpleCrimeType.Combat,
                    Filter.AllMurder => archetype.SimpleCrimeType is SimpleCrimeType.Murder,
                    Filter.Murder => archetype.Equals(CrimeArchetype.Murder(NpcValue)),
                    Filter.AllLockpicking => archetype.SimpleCrimeType is SimpleCrimeType.Lockpicking,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            
            enum Filter : byte {
                AllCrimes,
                AllTrespassing,
                AllTheftsOrPickpockets,
                AllTheftsOrPickpocketsByItem,
                AllThefts,
                Theft,
                AllPickpockets,
                AllPickpocketsByNpc,
                AllPickpocketsByItem,
                Pickpocket,
                AllCombat,
                AllMurder,
                Murder,
                AllLockpicking,
            }
        }
    }
}