using System;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur.SoulsOverview;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur {
    public partial class WyrdArthurUI : CharacterSubTab<VWyrdArthurUI>, IUnsavedChangesPopup {
        public Hero Hero => Hero.Current;
        public bool HasUnsavedChanges => Hero.Talents.AnyUnappliedTalentPoints();
        public VWyrdArthurUI View => View<VWyrdArthurUI>();
        CharacterSheetUI CharacterSheetUI => ParentModel.ParentModel;
        Talent CurrentTalent { get; set; }

        Prompt _promptAcquire;
        Prompt _promptReset;
        Prompt _promptConfirm;
        
        public static bool IsViewAvailable() => World.Any<WyrdSoulFragments>()?.UnlockedFragments.Contains(WyrdSoulFragmentType.Excalibur) ?? false;
        
        protected override void AfterViewSpawned(VWyrdArthurUI view) {
            CharacterSheetUI.SetHeroOnRenderVisible(false);
            World.EventSystem.ListenTo(EventSelector.AnySource, Talent.Events.TalentChanged, this, OnTalentChanged);
            
            if (TalentTree.IsUpgradeAvailable) {
                _promptAcquire = Prompt.Tap(KeyBindings.UI.Talents.AcquireTalent, LocTerms.UITalentsAcquire.Translate(), Acquire).AddAudio();
                _promptReset = Prompt.Tap(KeyBindings.UI.Talents.ResetTalent, LocTerms.UITalentsReset.Translate(), Reset).AddAudio();
                _promptConfirm = Prompt.Tap(KeyBindings.UI.Talents.ConfirmTalents, LocTerms.Confirm.Translate(), Confirm).AddAudio();
                CharacterSheetUI.Prompts.AddPrompt(_promptAcquire, this, false);
                CharacterSheetUI.Prompts.AddPrompt(_promptReset, this, false);
                CharacterSheetUI.Prompts.AddPrompt(_promptConfirm, this, false);
            }

            AddElement(new WyrdArthurPower());
        }

        void Confirm() {
            if (HasUnsavedChanges) {
                PopupUIFactory.ConfirmPopup(LocTerms.UIGenericConfirmChangesPopup.Translate(), Hero.Talents.ApplyTemporaryLevels, null);
            }
        }

        void OnTalentChanged(Talent talent) {
            if (CurrentTalent == talent) {
                RefreshPrompts();
            }
        }

        void Acquire() {
            CurrentTalent?.AcquireNextTemporaryLevel();
        }

        void Reset() {
            CurrentTalent?.DecrementTemporaryLevel();
        }

        public void RefreshPrompts(Talent talent) {
            CurrentTalent = talent;

            if (TalentTree.IsUpgradeAvailable) {
                RefreshPrompts();
            }
        }

        void RefreshPrompts() {
            _promptAcquire.SetActive(CurrentTalent is { CanBeUpgraded: true });
            _promptReset.SetActive(CurrentTalent is { CanBeReset: true });
            _promptConfirm.SetActive(HasUnsavedChanges);
        }
        
        public void ShowUnsavedPopup(Action continueCallback) {
            if (HasUnsavedChanges) {
                PopupUIFactory.CreateUnsavedChangesPopup(LocTerms.UIGenericUnsavedChangesPopup.Translate(), continueCallback, Hero.Talents.ApplyTemporaryLevels, Hero.Talents.ClearTemporaryPoints, null);
            } else {
                continueCallback.Invoke();
            }
        }
    }
}