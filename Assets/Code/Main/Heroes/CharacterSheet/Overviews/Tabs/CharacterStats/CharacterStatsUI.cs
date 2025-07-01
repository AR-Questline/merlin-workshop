using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.StatsSummary;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterStats {
    public partial class CharacterStatsUI : CharacterSubTab<VCharacterStatsUI>, IUnsavedChangesPopup {
        public bool HasUnsavedChanges => Elements<StatsEntryUI>().Sum(st => st.StatChangeValue) > 0;
        public Stat AvailablePoints => Hero.Development.BaseStatPoints;

        Hero Hero => CharacterSheetUI.Hero;
        HeroRPGStats HeroRPGStats => Hero.HeroRPGStats;
        CharacterSheetUI CharacterSheetUI => ParentModel.ParentModel;
        
        Prompt _promptIncrement;
        Prompt _promptDecrement;
        Prompt _promptConfirm;
        Prompt _escapePrompt;
        VCharacterStatsEntryInfoUI _unfoldedEntryInfo;
        Model _popup;

        public new static class Events {
            public static readonly Event<CharacterStatsUI, bool> NewCharacterStatsApplied = new(nameof(NewCharacterStatsApplied));
            public static readonly Event<CharacterStatsUI, StatPointsChange> PointsToApplyChange = new(nameof(PointsToApplyChange));
        }
        
        protected override void AfterViewSpawned(VCharacterStatsUI view) {
            CharacterSheetUI.SetHeroOnRenderVisible(false);

            if (TalentTree.IsUpgradeAvailable) {
                _promptDecrement = Prompt.VisualOnlyTap(KeyBindings.UI.Generic.DecreaseValue, LocTerms.UIDecrease.Translate()).AddAudio();
                _promptIncrement = Prompt.VisualOnlyTap(KeyBindings.UI.Generic.IncreaseValue, LocTerms.UIIncrease.Translate()).AddAudio();
                    
                CharacterSheetUI.Prompts.AddPrompt(_promptIncrement, this, false, false);
                CharacterSheetUI.Prompts.AddPrompt(_promptDecrement, this, false, false);

                _promptConfirm = Prompt.Tap(KeyBindings.UI.Talents.ConfirmTalents, LocTerms.Confirm.Translate(), Confirm).AddAudio();
                CharacterSheetUI.Prompts.AddPrompt(_promptConfirm, this, HasUnsavedChanges);
                
                this.ListenTo(Events.PointsToApplyChange, (tuple) => _promptConfirm.SetActive(tuple.hasPointsToApply), this);
            }
            
            AddElement(new StatsSummaryUI());
        }
        
        public void ShowUnsavedPopup(Action continueCallback) {
            if (HasUnsavedChanges) {
                PopupUIFactory.CreateUnsavedChangesPopup(LocTerms.UIGenericUnsavedChangesPopup.Translate(), continueCallback, ApplyStatValues, ResetStatValues, null);
            } else {
                continueCallback.Invoke();
            }
        }
        
        void Confirm() {
            if (HasUnsavedChanges) {
                PopupUIFactory.ConfirmPopup(LocTerms.UIGenericConfirmChangesPopup.Translate(), ApplyStatValues, null);
            }
        }
        
        public void Fold(VCharacterStatsEntryInfoUI entry) {
            if (_unfoldedEntryInfo == entry) return;
            
            if (_unfoldedEntryInfo) {
                _unfoldedEntryInfo.Unfold();
            }

            _unfoldedEntryInfo = entry;
            _unfoldedEntryInfo.Fold();
        }
        
        public void InitializeStatEntries() {
            foreach (var rpgStat in HeroRPGStats.GetHeroRPGStats()) {
                var statEntry = new StatsEntryUI(rpgStat);
                AddElement(statEntry);

                var heroStatType = (HeroRPGStatType) rpgStat.Type;
                statEntry.AddElement(new CharacterStatsEntryInfoUI(heroStatType));
                statEntry.View<VStatsEntryUI>().Button.OnHover += RefreshPrompts;
            }
        }
    
        void RefreshPrompts(bool isHovered) {
            if (TalentTree.IsUpgradeAvailable == false) return; 
            
            bool statsHovered = Elements<StatsEntryUI>().Any(entry => entry.View<VStatsEntryUI>().IsFolded);
            _promptIncrement.SetupState(RewiredHelper.IsGamepad, statsHovered);
            _promptDecrement.SetupState(RewiredHelper.IsGamepad, statsHovered);
        }

        void ApplyStatValues() {
            foreach (StatsEntryUI statsEntry in Elements<StatsEntryUI>()) {
                statsEntry.ApplyStatChange();
            }
            Hero.RefillBaseStats();
            this.Trigger(Events.NewCharacterStatsApplied, true);
        }
        
        void ResetStatValues() {
            foreach (StatsEntryUI statsEntry in Elements<StatsEntryUI>()) {
                statsEntry.ResetStatValue();
            }
        }
    }
}