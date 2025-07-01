using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Tabs {
    public interface ICharacterSheetTabWithSubTabs : IModel {
        bool TryToggleSubTab(CharacterSheetUI ui);
    }
}