using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [UnityEngine.Scripting.Preserve]
    public partial class GenericBossCombat : BaseBossCombat {
        public override ushort TypeForSerialization => SavedModels.GenericBossCombat;

        [SerializeField] bool changePhaseWithTranition = false;
        [SerializeField, Range(0f, 1f)] float changePhaseWhenHpBelowPercent = 0.5f;
        
        // === Initialization
        public override void InitFromAttachment(BossCombatAttachment spec, bool isRestored) {
            if (spec.BossBaseClass is not GenericBossCombat genericBossCombat) {
                Log.Critical?.Error("GenericBossCombat: Spec is not GenericBossCombat!");
                return;
            }
            changePhaseWithTranition = genericBossCombat.changePhaseWithTranition;
            changePhaseWhenHpBelowPercent = genericBossCombat.changePhaseWhenHpBelowPercent;
            base.InitFromAttachment(spec, isRestored);
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            NpcElement.OnCompletelyInitialized(OnNpcCompletelyInitialized);
        }

        void OnNpcCompletelyInitialized(NpcElement npc) {
            npc.ListenTo(Stat.Events.StatChanged(AliveStatType.Health), OnHealthChanged, this);
        }

        void OnHealthChanged(Stat stat) {
            if (CurrentPhase > 0) {
                return;
            }
            
            if (stat is not LimitedStat limitedStat) {
                return;
            }

            if (limitedStat.Percentage < changePhaseWhenHpBelowPercent) {
                if (changePhaseWithTranition) {
                    SetPhaseWithTransition(CurrentPhase + 1);
                } else {
                    IncrementPhase();
                }
            }
        }
    }
}