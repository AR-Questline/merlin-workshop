using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.Utility.Maths;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemQualityComponent : ItemSlotComponent {
        [SerializeField] Image image;

        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            float alpha = image.color.a;
            image.color = item.Quality.BgColor.Color.WithAlpha(alpha);
            SetInternalVisibility(true);
        }
    }
}