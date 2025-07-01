using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Saving.Models {
    [SpawnsView(typeof(VSaveLoadUnavailableInfo))]
    public partial class SaveLoadUnavailableInfo : Model {
        public string Reason { get; private set; }
        
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        public static void ShowSaveUnavailableInfo() {
            if (World.Only<HeroCombat>().IsHeroInFight) {
                ShowInfo(LocTerms.SavingBlockedInCombat);
            } else if (World.HasAny<SaveBlocker>()) {
                ShowInfo(LocTerms.SavingBlocked);
            } else {
                ShowInfo(LocTerms.SavingBlockedOther);
            }
        }

        public static void ShowLoadUnavailableInfo() {
            if (World.HasAny<SavingWorldMarker>()) {
                ShowInfo(LocTerms.LoadingBlockedWhileSaving);
            } else if (World.HasAny<LoadBlocker>()) {
                ShowInfo(LocTerms.LoadingBlocked);
            } else {
                ShowInfo(LocTerms.LoadingBlockedOther);
            }
        }

        static void ShowInfo(string reason) {
            var saveLoadUnavailableInfo = World.Any<SaveLoadUnavailableInfo>() ?? World.Add(new SaveLoadUnavailableInfo());
            saveLoadUnavailableInfo.UpdateInfo(reason);
        } 

        void UpdateInfo(string reason) {
            Reason = reason;
            this.TriggerChange();
        }
    }
}
