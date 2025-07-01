using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel {
    [UsesPrefab("Items/" + nameof(VItemsDefaultUI))]
    public class VItemsDefaultUI : VItemsUI, IItemsListTitle {
        [Title("Title")]
        [SerializeField] GameObject titleParent;
        [SerializeField] TMP_Text titleLabel;

        protected override void OnMount() {
            SetTitleActive(false);
        }

        public void SetupTitle(string title, string contextTitle = "") {
            titleLabel.text = string.IsNullOrEmpty(contextTitle) 
                ? title.ColoredText(ARColor.MainGrey).FontLight() 
                : $"{contextTitle.ColoredText(ARColor.MainGrey).FontLight()}: {title.Italic().ColoredText(ARColor.MainWhite).FontSemiBold()}";
            SetTitleActive(true);
        }
        
        public void SetTitleActive(bool active) {
            titleParent.SetActive(active);
        }
    }
}