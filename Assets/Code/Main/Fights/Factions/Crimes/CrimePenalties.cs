using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public static class CrimePenalties {
        const float JailedBountyPenaltyModifier = 1.5f;
        
        public static class Events {
            public static readonly Event<Hero, CrimeOwnerTemplate> CrimePenaltyGuardCaught = new(nameof(CrimePenaltyGuardCaught));
            public static readonly Event<Hero, CrimeOwnerTemplate> CrimePenaltyWentToJailPeacefully = new(nameof(CrimePenaltyWentToJailPeacefully));
            public static readonly Event<Hero, CrimeOwnerTemplate> CrimePenaltyWentToJailFromCombat = new(nameof(CrimePenaltyWentToJailFromCombat));
            public static readonly Event<Hero, CrimeOwnerTemplate> CrimePenaltyPayedBounty = new(nameof(CrimePenaltyPayedBounty));
        }

        public static void PayBounty(CrimeOwnerTemplate crimeOwner, float modifier = 1f) {
            crimeOwner = PrepFactionForUse(crimeOwner);
            
            Hero.Current.Trigger(Events.CrimePenaltyPayedBounty, crimeOwner);
            BountyPayInternal(crimeOwner, modifier);
            BountyLoseItemsInternal(crimeOwner);
        }
        
        public static void GoToPrisonPeacefully(CrimeOwnerTemplate crimeOwner, float bountyPenaltyModifer = JailedBountyPenaltyModifier) {
            crimeOwner = PrepFactionForUse(crimeOwner);
            
            Hero.Current.Trigger(Events.CrimePenaltyWentToJailPeacefully, crimeOwner);
            ApplyPrisonPunishment(crimeOwner, bountyPenaltyModifer);
            
            if (crimeOwner.Prison is not {IsSet: true}) {
                Log.Critical?.Error("No prison set for faction: '" + crimeOwner.name + "' !");
                return;
            }
            GoToPrisonDelayed(crimeOwner).Forget();
        }

        public static bool GoToPrisonFromCombat(CrimeOwnerTemplate crimeOwner) {
            crimeOwner = PrepFactionForUse(crimeOwner);
            if (CrimeUtils.HasCommittedUnforgivableCrime(crimeOwner)) {
                return false;
            }
            if (!CrimeUtils.HasBounty(crimeOwner)) {
                return false;
            }
            if (crimeOwner.Prison is not {IsSet: true}) {
                Log.Critical?.Error("No prison set for faction: '" + crimeOwner.name + "' !");
                return false;
            }
            
            Hero.Current.Trigger(Events.CrimePenaltyWentToJailFromCombat, crimeOwner);
            ApplyPrisonPunishment(crimeOwner);
            GoToPrison(crimeOwner);
            return true;
        }

        // === Helpers

        static CrimeOwnerTemplate PrepFactionForUse(CrimeOwnerTemplate crimeOwner) {
            CrimeOwnerUtils.GetCrimeOwnersOfRegion(CrimeType.Combat, Hero.Current.Coords, out var crimeOwners);
            if (!crimeOwners.IsEmpty) {
                crimeOwner = crimeOwners.PrimaryOwner;
            }

            TemporaryBounty.TryGet()?.GuardApplyCrimes();
            return crimeOwner;
        }
        
        static async UniTaskVoid GoToPrisonDelayed(CrimeOwnerTemplate crimeOwner) {
            if (await AsyncUtil.DelayFrame(Hero.Current)) {
                GoToPrison(crimeOwner);
            }
        }

        static void GoToPrison(CrimeOwnerTemplate crimeOwner) {
            World.Add(new DeathUI(crimeOwner));
        }

        static void ApplyPrisonPunishment(CrimeOwnerTemplate crimeOwner, float bountyPenaltyModifer = JailedBountyPenaltyModifier) {
            BountyPayInternal(crimeOwner, bountyPenaltyModifer);
            BountyLoseItemsInternal(crimeOwner);
            AddPrisonStatus();
        }

        static void BountyPayInternal(CrimeOwnerTemplate crimeOwner, float modifier) {
            float bounty = CrimeUtils.Bounty(crimeOwner) * modifier;
            Hero.Current.Wealth.DecreaseBy(bounty);
            CrimeUtils.ClearBounty(crimeOwner);
            AIUtils.ForceStopCombatWithHero();
        }

        static void BountyLoseItemsInternal(CrimeOwnerTemplate crimeOwner) {
            var stolenItems = Hero.Current.Inventory.Items
                .Where(i => StolenItemElement.IsStolenFrom(i, crimeOwner)).ToList();
            
            foreach (Item stolenItem in stolenItems) {
                Hero.Current.HeroItems.Remove(stolenItem);
            }
        }

        static void AddPrisonStatus() {
            var hero = Hero.Current;
            var statusTemplate = CommonReferences.Get.JailStatusTemplate;
            var sourceInfo = StatusSourceInfo.FromStatus(statusTemplate);
            hero.Statuses.AddStatus(statusTemplate, sourceInfo);
        }
    }
}
