using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.UI.Components.Tabs;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Character {
    public partial class CharacterUI : CharacterSheetTab<VCharacterUI>, CharacterSubTabs.ISubTabParent<VCharacterUI>, ICharacterSheetTabWithSubTabs {
        static CharacterSubTabType s_lastTab;
        public CharacterSubTabType CurrentType { get; set; } = CharacterSubTabType.None;
        public CharacterSubTabs.ISubTabParent<VCharacterUI> SubTabParent => this;
        public bool ForceInvisibleTab => true;
        public Tabs<CharacterUI, VCharacterTabs, CharacterSubTabType, ICharacterSubTab> TabsController { get; set; }

        protected override void AfterViewSpawned(VCharacterUI view) {
            ParentModel.SetHeroOnRenderVisible(false);
            AddElement(new CharacterSubTabs());
        }
        
        public static async UniTask<CharacterUI> ToggleCharacterSheet(CharacterSubTabType initialTab, bool ignoreMapState = false, CharacterSheetTabType[] availableTabs = null, bool backDestroysCurrentView = false) {
            var ui = CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Character, ignoreMapState, availableTabs);
            
            if (ui != null && await AsyncUtil.DelayFrame(ui)) {
                CharacterUI characterUI = ui.Element<CharacterUI>();
                characterUI.TabsController.SelectTab(initialTab);
                return characterUI;
            }

            return null;
        }
        
        public bool TryToggleSubTab(CharacterSheetUI ui) {
            if (s_lastTab == CharacterSubTabType.None) {
                return false;
            }
            
            ui.Element<CharacterUI>().TabsController.SelectTab(s_lastTab);
            return true;
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            s_lastTab = CurrentType;
        }
    }
}
