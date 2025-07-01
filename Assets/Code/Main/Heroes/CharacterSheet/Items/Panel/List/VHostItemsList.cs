using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    [UsesPrefab("Items/List/" + nameof(VHostItemsList))]
    public class VHostItemsList : View<ItemsListUI> {
        [SerializeField] Transform viewHost;
        public Transform ViewHost => viewHost;
    }
}