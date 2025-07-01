using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public static class CrimeUtils {
        const string MemoryContext = Faction.FactionContext + ".crime";
        static ContextualFacts CrimeFacts => World.Services.Get<GameplayMemory>().Context(MemoryContext);
        
        // === Fact Keys
        static class Keys {
            // Maybe add an on demand cache here
            public static string UnforgivableCrimeCommitted(CrimeOwnerTemplate template) => $"UnforgivableCrimeCommitted: {template.GUID}";
            public static string BountyValue(CrimeOwnerTemplate template) => $"Bounty: {template.GUID}";
        }
        

        // === Events
        public static class Events {
            public static readonly Event<Hero, CrimeChangeData> CrimeCommitted = new(nameof(CrimeCommitted));
            public static readonly Event<Hero, CrimeOwnerTemplate> UnforgivableCrimeCommittedAgainst = new(nameof(UnforgivableCrimeCommittedAgainst));
            public static readonly Event<Hero, CrimeOwnerTemplate> BountyClearedFor = new(nameof(BountyClearedFor));
            public static readonly Event<Hero, bool> RecalculateTrespassing = new(nameof(RecalculateTrespassing));
        }
        
        // === Accessors & Modifiers ===
        
        // --- Unforgivable Crime Committed against Faction
        public static bool HasCommittedUnforgivableCrime(CrimeOwnerTemplate template) {
            if (template == null) {
                return false;
            }
            return CrimeFacts.Get(Keys.UnforgivableCrimeCommitted(template), false);
        }

        public static void CommitUnforgivableCrime(CrimeOwnerTemplate template) {
            CrimeFacts.Set(Keys.UnforgivableCrimeCommitted(template), true);
            Hero.Current.Trigger(Events.UnforgivableCrimeCommittedAgainst, template);
        }

        // --- Faction Bounty
        public static bool HasBounty(CrimeOwnerTemplate template) {
            if (template == null) {
                return false;
            }
            return CrimeFacts.Get(Keys.BountyValue(template), 0f) > 0;
        }

        public static float Bounty(CrimeOwnerTemplate template) {
            if (template == null) {
                return 0f;
            }
            return CrimeFacts.Get(Keys.BountyValue(template), 0f);
        }

        public static bool IsCrimeFor(in Crime crime, CrimeOwnerTemplate template) {
            if (crime.Archetype.IsNoCrime || template == null) {
                return false;
            }
            return !template.IsAcceptable(crime.Archetype);
        }
        
        public static bool IsCrimeForAnyOwner(in Crime crime) {
            if (crime.Archetype.IsNoCrime) {
                return false;
            }
            foreach (var crimeOwner in crime.Owners.AllOwners) {
                if (IsCrimeFor(crime, crimeOwner)) {
                    return true;
                }
            }
            return false;
        }
        
        public static bool IsUnforgivableCrimeFor(in Crime crime, CrimeOwnerTemplate template) {
            if (crime.Archetype.IsNoCrime || template == null) {
                return false;
            }
            return template.IsUnforgivable(crime.Archetype);
        }
        
        public static bool TryCommitCrime(in Crime crime) {
            if (crime.Archetype.IsNoCrime) {
                return false;
            }
            
            var crimeOwners = crime.Owners;
            if (crimeOwners.IsEmpty) {
                return false;
            }

            bool anyCrime = false;
            bool evenIfNotSeen = crime.Situation.HasFlagFast(CrimeSituation.IgnoresVisibility);
            bool instantReport = crime.Situation.HasFlagFast(CrimeSituation.InstantReport);
            
            foreach (CrimeOwnerTemplate template in crimeOwners.AllOwners) {
                bool isCrime = TryCommitCrimeForOwner(crime, evenIfNotSeen, instantReport, template);
                anyCrime = anyCrime || isCrime;
            }
            return anyCrime;
        }

        static bool TryCommitCrimeForOwner(in Crime crime, bool evenIfNotSeen, bool instantReport, CrimeOwnerTemplate crimeOwner) {
            if (crimeOwner == null || !IsCrimeFor(crime, crimeOwner)) {
                return false;
            }

            bool isCrimeNoticed = false;

            if (!crime.Situation.HasFlagFast(CrimeSituation.SkipsWatchingNPCs)) {
                var watchingNpcs = Hero.Current.Element<IllegalActionTracker>().WatchingNpcs;
                
                if (watchingNpcs is {Count: > 0}) {
                    InformWatchingNPCs(crime, crimeOwner, watchingNpcs, ref instantReport, ref isCrimeNoticed);
                }
            }

            if (evenIfNotSeen) {
                isCrimeNoticed = true;
                CrimeReactionUtils.CallGuardsToHero(crimeOwner);
            }

            if (isCrimeNoticed) {
                Hero.Current.Trigger(IllegalActionTracker.Events.IllegalActivityPerformed, true);
                if (instantReport) {
                    CommitCrime(crime, crimeOwner);
                } else {
                    // Temp bounty should have been applied for witnesses in InformWatchingNPCs
                }
                return true;
            }

            return false;
        }

        
        static readonly List<NpcCrimeReactions> RelatedNPCs = new();
        static void InformWatchingNPCs(Crime crime, CrimeOwnerTemplate faction, List<NpcCrimeReactions> watchingNpcs, 
            ref bool instantReport,
            ref bool isCrimeNoticed) {
            
            RelatedNPCs.Clear();
            RelatedNPCs.EnsureCapacity(watchingNpcs.Count);
            var crimeType = crime.Archetype;

            FilterByFactionIntoRelated(faction, crimeType, watchingNpcs, ref instantReport);
                    
            if (!instantReport) {
                TemporaryBounty.GetOrCreate()
                    .RegisterCrime(RelatedNPCs.Select(n => n.ParentModel).ToList(), crime);
            }
                    
            for (var i = 0; i < RelatedNPCs.Count; i++) {
                NpcCrimeReactions reactions = RelatedNPCs[i];
                reactions.ReactToCrime(crimeType);

                isCrimeNoticed = true;
            }
        }

        static void FilterByFactionIntoRelated(CrimeOwnerTemplate template, CrimeArchetype archetype, List<NpcCrimeReactions> watchingNpcs, ref bool instantReport) {
            for (var i = 0; i < watchingNpcs.Count; i++) {
                NpcCrimeReactions reactions = watchingNpcs[i];
                if (CrimeReactionUtils.NPCContainsOwnerFaction(template, reactions, archetype)) {
                    if (reactions.IsGuard) {
                        instantReport = true;
                    }
                    RelatedNPCs.Add(reactions);
                }
            }
        }

        static void CommitCrime(in Crime crime, CrimeOwnerTemplate template) {
            if (template.HasBounty == false) {
                return;
            }
            
            float bounty = crime.Bounty(template);
            Log.Debug?.Info("[Thievery] Bounty: '" + bounty + "' for '" + template.name + "' with crime '" + crime.Archetype + "'");
            AddBounty(template, bounty, out var bountyChange);
            Hero.Current.Trigger(Events.CrimeCommitted, new() {
                CrimeCommitted = crime,
                Faction = template,
                BountyChange = bountyChange
            });
            
            if (template.IsUnforgivableCrimeBountyLimit((int) bountyChange.to) || IsUnforgivableCrimeFor(crime, template)) {
                CommitUnforgivableCrime(template);
            }
            
            // TODO: Add bounty of related factions
            CheckBountyThresholds(template, bountyChange);
        }
        
        public static void ClearBounty(CrimeOwnerTemplate template) {
            HeroCrimeWithProlong.RemoveProlongsForFaction(template);
            if (!HasBounty(template)) return;
            CrimeFacts.Set(Keys.BountyValue(template), 0);
            Hero.Current.Trigger(Events.BountyClearedFor, template);
        }
        
        public static void ClearUnforgivableCrime(CrimeOwnerTemplate template) {
            if (!HasCommittedUnforgivableCrime(template)) return;
            CrimeFacts.Set(Keys.UnforgivableCrimeCommitted(template), false);
            ClearBounty(template);
        }

        public static void AddBounty(CrimeOwnerTemplate template, float value, out Change<float> bountyChange) {
            var key = Keys.BountyValue(template);
            float previousBounty = CrimeFacts.Get(key, 0f);
            float newBounty = previousBounty + value;
            CrimeFacts.Set(key, newBounty);
            bountyChange = new Change<float>(previousBounty, newBounty);
        }

        // === Reactions ===
        static void CheckBountyThresholds(CrimeOwnerTemplate template, Change<float> bountyChange) { }
    }

    public readonly struct CrimeChangeData {
        public CrimeOwnerTemplate Faction { get; init; }
        public Crime CrimeCommitted { get; init; }
        public Change<float> BountyChange { [UnityEngine.Scripting.Preserve] get; init; }
        
        [UnityEngine.Scripting.Preserve]
        public CrimeChangeData(Crime crimeCommitted, CrimeOwnerTemplate faction, Change<float> bountyChange) {
            CrimeCommitted = crimeCommitted;
            Faction = faction;
            BountyChange = bountyChange;
        }
    }
}