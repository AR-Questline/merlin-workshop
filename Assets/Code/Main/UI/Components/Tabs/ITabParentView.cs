using System;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components.Tabs {
    public interface ITabParentView : IView {
        Transform TabButtonsHost { get; }
        Transform ContentHost { get; }
        
        void ToggleTabAndContent(bool showTabs);
        void HideTabs();
        void ShowTabs();
        void ShowContent();
        void HideContent();
    }
}