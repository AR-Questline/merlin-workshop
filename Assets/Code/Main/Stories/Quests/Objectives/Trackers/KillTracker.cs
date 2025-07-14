using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class KillTracker : BaseSimpleTracker<KillTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.KillTracker;

        NpcTemplate[] _allowedEnemyTemplate;
        ItemTemplate[] _usedWeaponTemplates;

        public override void InitFromAttachment(KillTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            
            _allowedEnemyTemplate = spec.AllowedEnemyTemplates?.ToArray();
            _usedWeaponTemplates = spec.UsedWeaponTemplate?.ToArray();
        }

        protected override void OnInitialize() {
            Hero.Current.ListenTo(HealthElement.Events.OnKill, OnHeroKilledSomething, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, HealthElement.Events.OnHeroSummonKill, this, OnHeroKilledSomething);
        }

        void OnHeroKilledSomething(DamageOutcome damageOutcome) {
            if (damageOutcome.Damage.Target is not NpcElement killedEnemy) {
                return;
            }

            if (!KilledEnemyMatchesTemplate(killedEnemy)) {
                return;
            }
            
            if (!UsedWeaponMatchesTemplate(damageOutcome.Damage.Item)) {
                return;
            }
            
            ChangeBy(1f);
        }

        bool KilledEnemyMatchesTemplate(NpcElement killedNpc) {
            if (_allowedEnemyTemplate == null) {
                return true;
            }

            foreach (var allowedEnemy in _allowedEnemyTemplate) {
                if (killedNpc.Template.InheritsFrom(allowedEnemy)) {
                    return true;
                }
            }
            
            return false;
        }
        
        bool UsedWeaponMatchesTemplate(Item usedWeapon) {
            if (_usedWeaponTemplates == null) {
                return true;
            }

            if (usedWeapon == null) {
                return false;
            }
            
            foreach (var allowedWeapon in _usedWeaponTemplates) {
                if (usedWeapon.Template.InheritsFrom(allowedWeapon)) {
                    return true;
                }
            }

            return false;
        }
    }
}
