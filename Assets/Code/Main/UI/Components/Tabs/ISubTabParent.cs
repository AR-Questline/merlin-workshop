using System;

namespace Awaken.TG.Main.UI.Components.Tabs {
    public interface ISubTabParent {
        void HandleBack(Action backCallback);
        void HandleUnsavedChanges(Action continueCallback);
    }
}