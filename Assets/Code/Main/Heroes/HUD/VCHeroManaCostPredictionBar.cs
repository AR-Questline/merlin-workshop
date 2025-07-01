using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroManaCostPredictionBar : ViewComponent<Hero> {
        [SerializeField] FillBar bar;
        [SerializeField] GlowEffect glowEffect;
        
        protected override void OnAttach() {
            glowEffect.Init();
            Target.ListenTo(Hero.Events.NotEnoughMana, IndicateNotEnoughMana, this);
        }
        
        void IndicateNotEnoughMana(float amount) {
            if (glowEffect.IsPlaying == false) {
                bar.SetPercentInstant(amount / Target.Mana.UpperLimit);
                glowEffect.StartGlow();
            }
        }
    }
}
