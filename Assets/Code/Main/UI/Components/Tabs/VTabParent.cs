using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components.Tabs {
    public abstract class VTabParent<TTarget> : View<TTarget>, ITabParentView where TTarget : IModel {
        [Title("Tab Settings")]
        [SerializeField] Transform tabButtons;
        [SerializeField] Transform contentHost;
        
        public Transform TabButtonsHost => tabButtons;
        public Transform ContentHost => contentHost;
        
        public virtual void ToggleTabAndContent(bool showTabs) {
            if (showTabs) {
                ShowTabs();
                HideContent();
            } else {
                HideTabs();
                ShowContent();
            }
        }
        
        public virtual void HideTabs() {
            TabButtonsHost.gameObject.SetActive(false);
        }
        
        public virtual void ShowTabs() {
            TabButtonsHost.gameObject.SetActive(true);
        }

        public virtual void ShowContent() {
            ContentHost.gameObject.SetActive(true);
        }
        
        public virtual void HideContent() {
            ContentHost.gameObject.SetActive(false);
        }
    }
}