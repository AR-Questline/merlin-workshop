using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class AliveIncreasedDamage : Element<Location>, IRefreshedByAttachment<AliveIncreasedDamageAttachment> {
        public override ushort TypeForSerialization => SavedModels.AliveIncreasedDamage;

        AliveIncreasedDamageAttachment _spec;
        public void InitFromAttachment(AliveIncreasedDamageAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            ParentModel.AfterFullyInitialized(() => {
                var alive = ParentModel.Element<IAlive>();
                var healthElement = alive.Element<HealthElement>();
                healthElement.ListenTo(HealthElement.Events.BeforeDamageTaken, TryApplyModifiers, this);
            });
        }

        void TryApplyModifiers(Damage dmg) {
            if (_spec.filterByNpcType && dmg.DamageDealer is NpcElement dealerNpc) {
                var dealerNpcType = dealerNpc.NpcType;
                foreach (var type in _spec.npcTypesFilter) {
                    if (dealerNpcType == type) {
                        dmg.AddBonusMultiplier(_spec.damageMultiplier);
                        return;
                    }
                }
            }

            if (_spec.applyToSpecificProjectiles) {
                var projectileType = dmg.Projectile?.GetType();
                if (projectileType != null) {
                    foreach (var type in _spec.specificProjectileTypes) {
                        if (projectileType == type) {
                            dmg.AddBonusMultiplier(_spec.damageMultiplier);
                            return;
                        }
                    }
                }
            }
        }
    }
}