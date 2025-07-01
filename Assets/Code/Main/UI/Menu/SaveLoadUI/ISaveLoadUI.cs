using Awaken.TG.MVC.UI.Handlers.Selections;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu.SaveLoadUI {
    public interface ISaveLoadUI : IUIStateSource {
        string TitleName { get; }
        Transform SlotsParent { get; }
        void OnSelectionChanged(SelectionChange selectionChange);
        void SaveLoadAction(SaveLoadSlotUI saveLoadSlotUI);
    }
}