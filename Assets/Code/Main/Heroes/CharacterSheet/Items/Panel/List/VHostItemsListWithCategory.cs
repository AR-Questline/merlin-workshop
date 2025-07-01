using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    [UsesPrefab("Items/List/" + nameof(VHostItemsListWithCategory))]
    public class VHostItemsListWithCategory : View<ItemsListUI> {
        [SerializeField] Transform viewHost;
        [SerializeField] RectTransform viewport;
        [SerializeField] ScrollRect categoriesScrollRect;
        [SerializeField] VCItemSorting sortView;

        public Transform ViewHost => viewHost;
        public RectTransform Viewport => viewport;
        public ScrollRect CategoriesScrollRect => categoriesScrollRect;
        public VCItemSorting SortView => sortView;

        public void ResizeToContent(float contentWidth) {
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(contentWidth, rect.sizeDelta.y);
        }
    }
}