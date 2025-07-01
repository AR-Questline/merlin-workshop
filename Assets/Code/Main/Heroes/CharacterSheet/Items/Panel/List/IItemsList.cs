using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    public interface IItemsList {
        Transform ItemHost { get; }
        int MaxColumnCount { get; }
        int MaxRowCount { get; }
        int DisplayColumnCount { get; }
        int? ItemsCount { get; set; }
        int? LastItemIndex { get; set; }
        int? FirstItemIndex { get; set; }

        void Refresh();
        void RefreshSelfState();
        void OrderChanged();
        void ChangeGrid(int columnCount = -1, int rowCount = -1);
    }
}