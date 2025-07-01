using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public abstract class VCStatBarWithFail : VCHeroHUDBar {
        [SerializeField] GlowEffect failGlowEffect;
        [SerializeField] public EventReference failSound;

        public override bool ForceShow => base.ForceShow || failGlowEffect.IsPlaying;

        protected override void OnAttach() {
            failGlowEffect.Init();
            Target.ListenTo(Hero.Events.StatUseFail, IndicateStatUseFail, this);

            base.OnAttach();
        }

        void IndicateStatUseFail(StatType statType) {
            if (statType != StatType) {
                return;
            }

            StatUseFailEffect();
        }

        void StatUseFailEffect() {
            if (!failGlowEffect.IsPlaying) {
                failGlowEffect.StartGlow();
                FMODManager.PlayOneShot(failSound);
            }
        }
    }
}