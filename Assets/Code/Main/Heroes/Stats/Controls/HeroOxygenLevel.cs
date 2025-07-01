using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.Stats.Controls {
    public partial class HeroOxygenLevel : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        [UnityEngine.Scripting.Preserve] public LimitedStat OxygenLevel => ParentModel.HeroStats.OxygenLevel;
        
        protected override void OnInitialize() {
            ParentModel.ListenTo(Stat.Events.StatChanged(HeroStatType.OxygenLevel), OnOxygenLevelChanged, this);
        }

        public new static class Events {
            public static readonly Event<HeroOxygenLevel, LimitedStat> OxygenLevelChanged = new(nameof(OxygenLevelChanged));
        }
        
        void OnOxygenLevelChanged(Stat stat) {
            var oxygenLevelStat = (LimitedStat)stat;
            this.Trigger(Events.OxygenLevelChanged, oxygenLevelStat);
            if (oxygenLevelStat.Percentage <= 0 && !ParentModel.HasElement<SuffocateStatus>()) {
                SuffocateStatus.AddToHero(ParentModel);
            }
        }
    }
}