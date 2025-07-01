using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.UI.RawImageRendering {
    [SpawnsView(typeof(VItemRenderer))]
    public partial class ItemRenderer : Element<Model> {
        public sealed override bool IsNotSaved => true;

        public Item ItemToRender { get; private set; }
        public ARAssetReference ModelToRender {
            get {
                if (ItemToRender.Template.DropPrefab?.IsSet ?? false) {
                    return ItemToRender.Template.DropPrefab.Get();
                }

                return ItemToRender.TryGetElement<ItemEquip>()?.GetDebugHeroItem();
            }
        }

        public Transform ViewParent { get; }
        public Vector3 PositionOffset { get; }

        public ItemRenderer(Item itemToRender, Transform viewParent, Vector3 positionOffset) {
            ItemToRender = itemToRender;
            ViewParent = viewParent;
            PositionOffset = positionOffset;
        }

        [UnityEngine.Scripting.Preserve]
        public void UpdateRenderedItem(Item itemToRender) {
            if (ItemToRender != itemToRender) {
                ItemToRender = itemToRender;
                TriggerChange();
            }
        }
    }
}