using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Thievery {
    public partial class HeroRescueOnDeath : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        protected override void OnFullyInitialized() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.BeforeTakenFinalDamage, OnTakingDamage, this);
        }

        void OnTakingDamage(HookResult<HealthElement, Damage> result) {
            if (result.Prevented) {
                return;
            }
            if (World.HasAny<DeathUI>()) {
                result.Prevent();
                return;
            }
            if (result.Value.Amount < result.Model.Health.ModifiedValue) {
                return;
            }
            
            if (TryTeleportToPrison(result)) {
                return;
            }

            if (TryRescueFromAttachments(result)) {
                return;
            }
        }

        bool TryTeleportToPrison(HookResult<HealthElement, Damage> result) {
            ICharacter valueDamageDealer = result.Value.DamageDealer;
            var faction = valueDamageDealer?.DefaultCrimeOwner;
            CrimeOwnerUtils.GetCrimeOwnersOfRegion(CrimeType.Combat, Hero.Current.Coords, out var owners);
            if (!owners.IsEmpty) {
                faction = owners.PrimaryOwner;
            }

            if (CrimePenalties.GoToPrisonFromCombat(faction)) {
                PreventDeath(result);
                return true;
            }
            return false;
        }

        bool TryRescueFromAttachments(HookResult<HealthElement, Damage> result) {
            var damageDealer = result.Value.DamageDealer;
            if (damageDealer is NpcElement npc) {
                if (npc.ParentModel.TryGetElement<TeleportHeroOnHeroKill>(out var teleportHeroOnHeroKill)) {
                    PreventDeath(result);
                    teleportHeroOnHeroKill.HeroKilled();
                    return true;
                }
            }
            var receiver = result.Model.ParentModel;
            if (receiver is Hero hero) {
                if (hero.TryGetElement<TeleportHeroOnHeroDeath>(out var teleportHeroOnHeroDeath)) {
                    PreventDeath(result);
                    teleportHeroOnHeroDeath.HeroKilled();
                    return true;
                }
            }
            return false;
        }

        void PreventDeath(HookResult<HealthElement, Damage> result) {
            result.Prevent();
            result.Model.Health.SetTo(1f);
        }
    }
}