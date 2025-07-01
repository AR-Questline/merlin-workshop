using Awaken.TG.MVC.Attributes;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    [UsesPrefab("Items/List/" + nameof(VItemsListWithTitleUI))]
    public class VItemsListWithTitleUI : VBaseItemsListUI, IItemsListTitle {
        [Title("Title")]
        [SerializeField] GameObject titleParent;
        [SerializeField] TextMeshProUGUI title;
        
        protected override void ConfigureList() {
            SetTitleActive(!string.IsNullOrEmpty(title.text));
            base.ConfigureList();
        }

        public void SetupTitle(string titleLabel, string contextTitle = "") {
            title.text = titleLabel;
            SetTitleActive(true);
        }

        public void SetTitleActive(bool active) {
            titleParent.SetActive(active);
        }
    }
}