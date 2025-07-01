using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroManaReservationBar : ViewComponent<Hero> {
        [SerializeField] Bar bar;
        
        protected override void OnAttach() {
            bar.SetPercentInstant(0);
            
            Target.ListenTo(Stat.Events.StatChanged(HeroStatType.MaxManaReservation), Refresh, this);
            Target.ListenTo(Stat.Events.StatChanged(CharacterStatType.MaxMana), Refresh, this);
            Target.ListenTo(Stat.Events.StatChanged(CharacterStatType.Mana), Refresh, this);
        }
        
        void Refresh(Stat stat) { 
            bar.SetPercentInstant(Target.MaxManaReservation?.ModifiedValue / Target.MaxManaWithReservation ?? 0);
        }
    }
}
