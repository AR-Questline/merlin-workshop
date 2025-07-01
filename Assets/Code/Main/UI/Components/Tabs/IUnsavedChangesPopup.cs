using System;

namespace Awaken.TG.Main.UI.Components.Tabs {
    public interface IUnsavedChangesPopup {
        bool HasUnsavedChanges { get; }
        void ShowUnsavedPopup(Action continueCallback);
    }
}
