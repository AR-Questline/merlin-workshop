using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;
using System.Collections.Generic;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs {
    [UsesPrefab("Items/Tabs/" + nameof(VItemsTabs))]
    public class VItemsTabs : View<ItemsTabs> {
        [SerializeField] Transform verticalRoot;
        [SerializeField] Transform horizontalRoot;
        [SerializeField] Transform tabsHost;

        readonly List<Transform> _tabs = new();
        
        protected override void OnMount() {
            verticalRoot.gameObject.SetActive(false);
            horizontalRoot.gameObject.SetActive(false);
            
            foreach (Transform tab in tabsHost) {
                _tabs.Add(tab);
            }

            if (Target.ParentModel.Config.TabsPosition is LayoutPosition.Left or LayoutPosition.Right) {
                SetTabsParent(0, verticalRoot);
            } else {
                SetTabsParent(1, horizontalRoot);
            }
        }

        void SetTabsParent(int startIndex, Transform parent) {
            for (int i = 0; i < _tabs.Count; i++) {
                _tabs[i].SetParent(parent);
                _tabs[i].SetSiblingIndex(startIndex + i);
            }
            
            parent.gameObject.SetActive(true);
        }
    }
}