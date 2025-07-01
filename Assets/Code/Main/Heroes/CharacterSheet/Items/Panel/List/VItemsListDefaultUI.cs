using Awaken.TG.MVC.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    [UsesPrefab("Items/List/" + nameof(VItemsListDefaultUI))]
    public class VItemsListDefaultUI : VBaseItemsListUI {
        [Title("Sorting")]
        [SerializeField] GameObject sortingParent;
        
        protected override void ConfigureList() {
            base.ConfigureList();
            sortingParent.SetActive(!Target.IsEmpty && !Target.IsMultipleLists);
        }
    }
}