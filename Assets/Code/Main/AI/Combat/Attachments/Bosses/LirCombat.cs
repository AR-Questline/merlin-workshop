using System;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.SkinnedBones;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [UnityEngine.Scripting.Preserve]
    [Serializable]
    public partial class LirCombat : GenericBossCombat {
        public override ushort TypeForSerialization => SavedModels.LirCombat;
        
        [SerializeField] float staggerOnBallistaHitDuration = 15f;

        // === Initialization
        public override void InitFromAttachment(BossCombatAttachment spec, bool isRestored) {
            if (spec.BossBaseClass is not LirCombat lirBossCombat) {
                Log.Critical?.Error("LirCombat: Spec is not LirCombat!");
                return;
            }
            staggerOnBallistaHitDuration = lirBossCombat.staggerOnBallistaHitDuration;
            base.InitFromAttachment(spec, isRestored);
        }

        protected override void OnPhaseTransitionStarted(int phase) {
            if (phase == 1) {
                var materialFadeController =
                    ParentModel.ViewParent.GetComponentInChildren<ClothToStitchMaterialFadeController>();
                if (materialFadeController) {
                    materialFadeController.FadeIn();
                }
            }
            base.OnPhaseTransitionFinished(phase);
        }

        protected override void OnDamageTaken(DamageOutcome damageOutcome) {
            base.OnDamageTaken(damageOutcome);
            
            if (NpcElement.IsDying || Staggered) {
                return;
            }

            if (CurrentBehaviour.Get() is LirHitScanBehaviour 
                && damageOutcome.Damage.Projectile is BallistaArrow 
                && damageOutcome.Damage.Item == Hero.Current.MainHandItem) {
                EnterStagger(staggerOnBallistaHitDuration);
                var damageParams = DamageParameters.Default;
                damageParams.Inevitable = true;
                var damageData = new RawDamageData(NpcElement.HealthElement.MaxHealth.ModifiedValue * 0.1f);
                NpcElement.HealthElement.TakeDamage(new Damage(damageParams, Hero.Current, NpcElement, damageData));
            }
        }
    }
}