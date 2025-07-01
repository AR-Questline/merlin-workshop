using System;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    [Flags]
    public enum CrimeSituation : byte {
        None = 0,
        IgnoresVisibility = 1 << 0,
        SkipsWatchingNPCs = 1 << 1,
        InstantReport = 1 << 2,
    }
    
    public struct Crime {
        float _bountyMultiplier;
        public CrimeArchetype Archetype { get; }
        public CrimeOwners Owners { get; }
        public CrimeSituation Situation { get; set; }

        public readonly float Bounty(CrimeOwnerTemplate crimeOwner) => Archetype.Bounty(crimeOwner) * _bountyMultiplier;

        Crime(in CrimeArchetype archetype, ICrimeSource source, float bountyMultiplier, CrimeSituation situation = CrimeSituation.None) {
            _bountyMultiplier = bountyMultiplier;
            Archetype = source.OverrideArchetype(archetype);
            Owners = source.GetCurrentCrimeOwnersFor(Archetype);
            Situation = situation;
        }
        
        public static Crime Theft(Item item, ICrimeSource source) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Theft(item);
            return new(crimeArchetype, source, item.Quantity * source.GetBountyMultiplierFor(crimeArchetype));
        }

        public static Crime Theft(ItemSpawningDataRuntime data, ICrimeSource source) {
            ItemTemplate dataItemTemplate = data.ItemTemplate;
            if (dataItemTemplate == null) {
                return new Crime(CrimeArchetype.None, source, 0);
            }
            CrimeArchetype crimeArchetype = CrimeArchetype.Theft(dataItemTemplate.CrimeValue);
            return new(crimeArchetype, source, data.quantity * source.GetBountyMultiplierFor(crimeArchetype));
        }

        public static Crime Theft(MountElement mount, ICrimeSource source) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Theft(mount.MountData.GameplayData.crimeValue);
            return new(crimeArchetype, source, source.GetBountyMultiplierFor(crimeArchetype));
        }

        public static Crime Theft(CrimeItemValue crimeValue, ICrimeSource source, int quantity) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Theft(crimeValue);
            return new(crimeArchetype, source, quantity * source.GetBountyMultiplierFor(crimeArchetype));
        }

        public static Crime Pickpocket(Item item, NpcElement owner) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Pickpocketing(item, owner.CrimeValue);
            return new(crimeArchetype, owner.ParentModel, item.Quantity * ((ICrimeSource) owner.ParentModel).GetBountyMultiplierFor(crimeArchetype));
        }

        public static Crime Pickpocket(Item item, CrimeNpcValue npcValue, ICrimeSource source) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Pickpocketing(item, npcValue);
            return new(crimeArchetype, source, item.Quantity * source.GetBountyMultiplierFor(crimeArchetype));
        }

        public static Crime Pickpocket(NpcElement owner) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Pickpocketing(CrimeItemValue.Low, owner.CrimeValue);
            return new(crimeArchetype, owner.ParentModel, ((ICrimeSource) owner.ParentModel).GetBountyMultiplierFor(crimeArchetype));
        }

        public static Crime Pickpocket(CrimeItemValue itemCrimeValue, CrimeNpcValue npcCrimeValue, ICrimeSource source, int quantity) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Pickpocketing(itemCrimeValue, npcCrimeValue);
            return new(crimeArchetype, source, quantity * source.GetBountyMultiplierFor(crimeArchetype));
        }

        public static Crime Combat(NpcElement npc, CrimeSituation situation = CrimeSituation.None) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Combat(npc.CrimeValue);
            return new(crimeArchetype, npc.ParentModel, ((ICrimeSource) npc.ParentModel).GetBountyMultiplierFor(crimeArchetype), situation);
        }

        public static Crime Combat(CrimeNpcValue crimeValue, ICrimeSource source, CrimeSituation situation = CrimeSituation.None) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Combat(crimeValue);
            return new(crimeArchetype, source, source.GetBountyMultiplierFor(crimeArchetype), situation);
        }

        public static Crime Murder(IWithCrimeNpcValue withCrimeNpcValue) =>
            new(CrimeArchetype.Murder(withCrimeNpcValue.CrimeValue), withCrimeNpcValue.ParentModel, 1);
        
        public static Crime Murder(CrimeNpcValue crimeValue, ICrimeSource source) {
            CrimeArchetype crimeArchetype = CrimeArchetype.Murder(crimeValue);
            return new(crimeArchetype, source, source.GetBountyMultiplierFor(crimeArchetype));
        }

        public static Crime Trespassing(ICrimeSource source) =>
            new(CrimeArchetype.Trespassing, source, source.GetBountyMultiplierFor(CrimeArchetype.Trespassing));
        
        public static Crime Lockpicking(ICrimeSource source) =>
            new(CrimeArchetype.Lockpicking, source, source.GetBountyMultiplierFor(CrimeArchetype.Lockpicking));

        public static Crime Custom(ICrimeSource source, CrimeSituation situation = CrimeSituation.None) =>
            new(CrimeArchetype.Custom, source, source.GetBountyMultiplierFor(CrimeArchetype.Custom), situation);

        public readonly bool IsCrime() => CrimeUtils.IsCrimeForAnyOwner(this);

        public bool TryCommitCrime(CrimeSituation situationAppend) {
            Situation = Situation.Append(situationAppend);
            return CrimeUtils.TryCommitCrime(this);
        }
        public readonly bool TryCommitCrime() => CrimeUtils.TryCommitCrime(this);

        [UnityEngine.Scripting.Preserve] public readonly bool IsPickpocketing => Archetype.SimpleCrimeType == SimpleCrimeType.Pickpocket;
        [UnityEngine.Scripting.Preserve] public readonly bool IsLockpicking => Archetype.SimpleCrimeType == SimpleCrimeType.Lockpicking;
        [UnityEngine.Scripting.Preserve] public readonly bool IsCombat => Archetype.SimpleCrimeType == SimpleCrimeType.Combat;
        [UnityEngine.Scripting.Preserve] public readonly bool IsTheft => Archetype.SimpleCrimeType == SimpleCrimeType.Theft;
        [UnityEngine.Scripting.Preserve] public readonly bool IsTrespassing => Archetype.SimpleCrimeType == SimpleCrimeType.Trespassing;
        [UnityEngine.Scripting.Preserve] public readonly bool IsMurder => Archetype.SimpleCrimeType == SimpleCrimeType.Murder;
    }
}