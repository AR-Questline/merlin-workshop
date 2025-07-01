using System;
using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [Serializable]
    public partial class StagfatherCombat : GenericBossCombat {
        public override ushort TypeForSerialization => SavedModels.StagfatherCombat;

        [SerializeField] float maxHpPerSecondHealOnPhaseChange = 0.2f;
        public SummonGroupOfAlliesBehaviour SummonBehaviour => TryGetElement<SummonGroupOfAlliesBehaviour>();

        public override void InitFromAttachment(BossCombatAttachment spec, bool isRestored) {
            if (spec.BossBaseClass is not StagfatherCombat stagfatherCombat) {
                Log.Critical?.Error("StagfatherCombat: Spec is not StagfatherCombat!");
                return;
            }
            maxHpPerSecondHealOnPhaseChange = stagfatherCombat.maxHpPerSecondHealOnPhaseChange;
        }

        protected override void AfterVisualLoaded(Transform transform) {
            base.AfterVisualLoaded(transform);
            this.ListenTo(SummonGroupOfAlliesBehaviour.Events.AllSummonsKilled, OnAllSummonsKilled, this);
        }

        protected override void OnPhaseTransitionFinished(int phase) {
            if (phase == 1) {
                StartBehaviour(SummonBehaviour);
                Heal().Forget();
            }
        }

        async UniTaskVoid Heal() {
            var armorTweak = new StatTweak(NpcElement.Stat(AliveStatType.Armor), 100, TweakPriority.Override, parentModel: NpcElement);
            armorTweak.MarkedNotSaved = true;
            
            var hp = NpcElement.Health;
            var hpPerSecondHeal = NpcElement.MaxHealth.ModifiedValue * maxHpPerSecondHealOnPhaseChange;
            
            while (hp is { Percentage: < 1f }) {
                hp.IncreaseBy(hpPerSecondHeal * Time.deltaTime);
                if (!await AsyncUtil.DelayFrame(NpcElement)) {
                    return;
                }
            }
            
            armorTweak.Discard();
        }

        void OnAllSummonsKilled() {
            var parameters = DamageParameters.Default;
            parameters.Inevitable = true;
            parameters.DamageTypeData = new RuntimeDamageTypeData(DamageType.Environment, DamageSubType.Pure);
            NpcElement.HealthElement.TakeDamage(new Damage(parameters, NpcElement, NpcElement, new RawDamageData(NpcElement.Health.ModifiedValue)));
        }
    }
}