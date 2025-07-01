using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Inventory;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal;
using Awaken.TG.Main.Heroes.CharacterSheet.Map;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Quests.UI;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Tabs {
    public partial class CharacterSheetTabs : Tabs<CharacterSheetUI, VCharacterSheetTabs, CharacterSheetTabType, ICharacterSheetTab> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.Previous;
        protected override KeyBindings Next => KeyBindings.UI.Generic.Next;

        protected override UIResult OnHandle(UIEvent evt) {
            if (evt is UIKeyDownAction action) {
                if (action.Name == KeyBindings.UI.CharacterSheets.CharacterSheet) {
                    ToggleTab(ParentModel.CurrentType);
                    return UIResult.Accept;
                }
                if (action.Name == KeyBindings.UI.CharacterSheets.Inventory) {
                    ToggleTab(CharacterSheetTabType.Inventory);
                    return UIResult.Accept;
                }
                if (action.Name == KeyBindings.UI.CharacterSheets.Journal) {
                    ToggleTab(CharacterSheetTabType.Journal);
                    return UIResult.Accept;
                }
                if (action.Name == KeyBindings.UI.CharacterSheets.QuestLog) {
                    ToggleTab(CharacterSheetTabType.Quests);
                    return UIResult.Accept;
                }
                if (!RewiredHelper.IsGamepad && action.Name == KeyBindings.UI.HUD.OpenSkillTree && TalentOverviewUI.IsViewAvailable()) {
                    var characterUI = ParentModel.TryGetElement<CharacterUI>();
                    var parentTab = CharacterSheetTabType.Character;
                    var targetTab = CharacterSubTabType.Talents;
                    
                    if (characterUI?.CurrentType == targetTab) {
                        TryHandleUnsavedTabChanges(ParentModel.TryDiscard);
                    } else {
                        if (ParentModel.CurrentType != parentTab) {
                            ChangeTab(parentTab);
                            characterUI = ParentModel.TryGetElement<CharacterUI>();
                        }
                        characterUI?.TabsController.SelectTab(targetTab);
                    }
                    return UIResult.Accept;
                }
                if (action.Name == KeyBindings.UI.CharacterSheets.ToggleMap && MapUI.IsOnSceneWithMap() && !RewiredHelper.IsGamepad) {
                    ToggleTab(CharacterSheetTabType.Map);
                    return UIResult.Accept;
                }
            }
            return UIResult.Ignore;
        }

        void ToggleTab(CharacterSheetTabType tab) {
            if (ParentModel.HeroRenderer.IsLoading) {
                return;
            }
            
            if (ParentModel.CurrentType == tab) {
                TryHandleUnsavedTabChanges(ParentModel.TryDiscard);
            } else {
                ChangeTab(tab);
            }
        }

        protected override void ChangeTab(CharacterSheetTabType type) {
            if (ParentModel.HeroRenderer.IsLoading) {
                return;
            }
            //handle click on tab with subtabs
            if (ParentModel.CurrentType == CharacterSheetTabType.Character && type == CharacterSheetTabType.Character) {
                TryHandleUnsavedTabChanges(() =>
                    ParentModel.Element<CharacterUI>().Element<CharacterSubTabs>().SetNone());
            } else if (ParentModel.CurrentType == CharacterSheetTabType.Journal && type == CharacterSheetTabType.Journal) {
                ParentModel.Element<JournalUI>().BackToMainTab();
            } else {
                base.ChangeTab(type);
            }
        }
    }
    
    public class CharacterSheetTabType : CharacterSheetTabs.DelegatedTabTypeEnum {
        public static readonly CharacterSheetTabType
            Character = new(nameof(Character), _ => new CharacterUI(), LocTerms.CharacterTabCharacter),
            Inventory = new(nameof(Inventory), _ => new InventoryUI(), LocTerms.CharacterTabInventory),
            Map = new(nameof(Map), _ => new MapUI(), LocTerms.CharacterTabMap),
            Quests = new(nameof(Quests), _ => new QuestLogUI(), LocTerms.CharacterTabQuests),
            Journal = new(nameof(Journal), _ => new JournalUI(), LocTerms.CharacterTabJournal);
        
        CharacterSheetTabType(string enumName, SpawnDelegate spawn, string titleID) : base(enumName, titleID, spawn, Always) { }
        
        public static readonly CharacterSheetTabType[] MapOnlyTabs = { CharacterSheetTabType.Map };
        public static readonly CharacterSheetTabType[] LevelUpTabs = { CharacterSheetTabType.Character };
        
        // we handle tab visibility using this method rather than the visible delegate passed to the constructor
        public override bool IsVisible(CharacterSheetUI target) {
            bool visible = target.OverrideAvailableTabs.IsNullOrEmpty() || target.OverrideAvailableTabs.Contains(this);
            if (this == Map) {
                return MapUI.IsOnSceneWithMap() && visible;
            }

            if (this == Journal) {
                return !PlatformUtils.IsJournalDisabled && visible;
            }
            
            return visible;
        }
    }
    
    public interface ICharacterSheetTab : CharacterSheetTabs.ITab { }

    public abstract partial class CharacterSheetTab<TTabView> : CharacterSheetTabs.Tab<TTabView>, ICharacterSheetTab where TTabView : View { }
}