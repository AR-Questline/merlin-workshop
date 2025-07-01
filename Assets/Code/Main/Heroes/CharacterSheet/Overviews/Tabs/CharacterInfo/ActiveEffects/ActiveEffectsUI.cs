using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.ActiveEffects {
    [SpawnsView(typeof(VActiveEffectsUI))]
    public partial class ActiveEffectsUI : Element<CharacterInfoUI> {
        public sealed override bool IsNotSaved => true;

        Hero Hero => ParentModel.CharacterSheetUI.Hero;
        ModelsSet<Status> HeroStatuses => Hero.Element<CharacterStatuses>().AllStatuses;
        
        public void InitializeEffectEntries() {
            foreach (var heroStatus in HeroStatuses.Where(ValidStatus)) {
                bool isAlreadyBeingShown = Elements<ActiveEffectEntryUI>().Any(existing =>
                        existing.heroStatus.SourceInfo.SourceUniqueID == heroStatus.SourceInfo.SourceUniqueID);
                    
                if (isAlreadyBeingShown) continue;
                

                var effectEntry = new ActiveEffectEntryUI(heroStatus);
                AddElement(effectEntry);

                var description = heroStatus.StatusDescription;
                effectEntry.AddElement(new EntryInfoUI(description, typeof(VEffectEntryInfoUI)));
            }
        }

        static bool ValidStatus(Status s) {
            if (s.HiddenOnUI) return false;
            if (s is BuildupStatus { Active: false }) return false;
            return s is {
                SourceInfo: {
                    Icon: { IsSet: true },
                    DisplayNameString: not null and not "",
                    HiddenOnUI: false
                }
            };
        }
    }
}