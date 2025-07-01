using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public static class CommitCrime {
        public static class Events {
            public static readonly Event<Hero, Item> PickpocketSuccess = new(nameof(PickpocketSuccess));
            public static readonly Event<Hero, Item> PickpocketFail = new(nameof(PickpocketFail));
        }

        public static bool Theft(Item item, ICrimeSource source) {
            var crime = Crime.Theft(item, source);
            if (!crime.IsCrime()) {
                return false;
            }
            
            if (item.Quality != ItemQuality.Quest && !item.HasElement<StolenItemElement>()) {
                item.AddElement(new StolenItemElement(crime));
            }
            return crime.TryCommitCrime();
        }
        
        public static bool Theft(MountElement mount, ICrimeSource source) {
            var crime = Crime.Theft(mount, source);
            return crime.IsCrime() && crime.TryCommitCrime();
        }

        public static bool Pickpocket(Item item, NpcElement owner) {
            var crime = Crime.Pickpocket(item, owner);
            if (!crime.IsCrime()) {
                return false;
            }
            
            NpcCrimeReactions reactions = owner.Element<NpcCrimeReactions>();
            
            bool noticed = false;
            if (reactions.ShouldReactToBeingPickpocketed(crime)) {
                reactions.SetSeeingHero(true);
            }
            
            if (crime.TryCommitCrime()) {
                noticed = true;
            }
            
            if (item.Quality != ItemQuality.Quest && !item.HasElement<StolenItemElement>()) {
                item.AddElement(new StolenItemElement(crime));
            }

            if (noticed) {
                // NPC now reacts to pickpocketing through the crime comitted
                PickpocketAction.lastPickpocketFailTime = Time.time;
                Hero.Current.Trigger(Events.PickpocketFail, item);
                return true;
            }
            Hero.Current.Trigger(Events.PickpocketSuccess, item);
            return false;
        }

        public static bool Trespassing(ICrimeSource source) {
            return Crime.Trespassing(source).TryCommitCrime();
        }
        
        public static bool Lockpicking(ICrimeSource source) {
            return Crime.Lockpicking(source).TryCommitCrime();
        }
        
        public static bool Combat(NpcElement npc, CrimeSituation situation = CrimeSituation.None) {
            Crime combat = Crime.Combat(npc, situation);
            if (!npc.IsUnconscious) {
                npc.Element<NpcCrimeReactions>().SetSeeingHero(true);
            }
            return combat.TryCommitCrime();
        }
        
        public static bool Murder(IWithCrimeNpcValue withCrime) {
            return Crime.Murder(withCrime).TryCommitCrime();
        }

        public static CrimeSituation Append(this CrimeSituation situation, CrimeSituation situationToAppend) {
            return situation | situationToAppend;
        }
        
        public static CrimeSituation Append(this CrimeSituation situation, bool ignoresVisibility = false, bool skipsWatchingNPCs = false, bool instantReport = false) {
            situation |= ignoresVisibility ? CrimeSituation.IgnoresVisibility : CrimeSituation.None;
            situation |= skipsWatchingNPCs ? CrimeSituation.SkipsWatchingNPCs : CrimeSituation.None;
            situation |= instantReport ? CrimeSituation.InstantReport : CrimeSituation.None;
            
            return situation;
        }

        [Pure]
        public static CrimeSituation GetSituation(bool ignoresVisibility = false, bool ignoresOwnership = false, bool instantReport = false) {
            return CrimeSituation.None.Append(ignoresVisibility, ignoresOwnership, instantReport);
        }
    }
}