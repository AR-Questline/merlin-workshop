using Awaken.Utility;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours.Mistbearer;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [Serializable]
    public partial class MistbearerCombatCopy : MistbearerCombatBase {
        public override ushort TypeForSerialization => SavedModels.MistbearerCombatCopy;

        ModelsSet<IBehaviourBase> _cachedBehaviours;

        protected override ModelsSet<IBehaviourBase> Behaviours {
            get {
                if (!BaseBehavioursLoaded) {
                    return ModelsSet<IBehaviourBase>.Empty;
                }
                if (!_cachedBehaviours.IsCreated) {
                    _cachedBehaviours = new ModelsSet<IBehaviourBase>(new List<IModel>(1) { Element<MistbearerWaitBehaviour>() });
                }
                return _cachedBehaviours;
            }
        }

        public void CopyMistbearerStats(MistbearerCombat mistbearerCombat) {
            NpcElement.Stat(AliveStatType.MaxHealth).SetTo(mistbearerCombat.NpcElement.Stat(AliveStatType.MaxHealth).ModifiedValue);
            NpcElement.Stat(AliveStatType.Health).SetTo(mistbearerCombat.NpcElement.Stat(AliveStatType.Health).ModifiedValue);
            NpcElement.Stat(CharacterStatType.Stamina).SetTo(mistbearerCombat.NpcElement.Stat(CharacterStatType.Stamina).ModifiedValue);
            SetPhase(mistbearerCombat.CurrentPhase);
        }

        public override void StartTeleportBehaviour() {
            ParentModel.Kill();
        }
        
        protected override void OnDamageTaken(DamageOutcome damageOutcome) {
            if (damageOutcome.Damage.IsPrimary) {
                ParentModel.Kill();
            }
        }

        // Mistbearer Copies
        public override void SpawnNewCopies(TeleportDestination[] copyDestinations) {
            ParentModel.Kill();
        }
    }
}