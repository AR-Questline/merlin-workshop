using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.ActiveEffects {
    [SpawnsView(typeof(VActiveEffectEntryUI))]
    public partial class ActiveEffectEntryUI : Element<ActiveEffectsUI> {
        public sealed override bool IsNotSaved => true;

        public readonly Status heroStatus;
        
        public ActiveEffectEntryUI(Status heroStatus) {
            this.heroStatus = heroStatus;
        }
    }
}