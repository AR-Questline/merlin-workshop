namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    public interface IItemsListTitle {
        void SetupTitle(string title, string contextTitle = null);
        void SetTitleActive(bool active);
    }
}
