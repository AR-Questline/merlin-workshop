using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroHealthReservationBar : ViewComponent<Hero> {
        [SerializeField] Bar bar;
        
        protected override void OnAttach() {
            bar.SetPercentInstant(0);
            
            Target.ListenTo(Stat.Events.StatChanged(HeroStatType.MaxHealthReservation), Refresh, this);
            Target.ListenTo(Stat.Events.StatChanged(AliveStatType.MaxHealth), Refresh, this);
            Target.ListenTo(Stat.Events.StatChanged(AliveStatType.Health), Refresh, this);
        }
        
        void Refresh(Stat stat) { 
            bar.SetPercentInstant(Target.MaxHealthReservation?.ModifiedValue / Target.MaxHealthWithReservation ?? 0);
        }
    }
}
