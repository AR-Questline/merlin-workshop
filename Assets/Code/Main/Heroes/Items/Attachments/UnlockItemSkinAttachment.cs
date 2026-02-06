using System;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "for items that unlocks a selected item skin in the transmog system when the selected item action is performed.")]
    public class UnlockItemSkinAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, Required, TemplateType(typeof(ItemTemplate))]
        TemplateReference[] skinItems = Array.Empty<TemplateReference>();
        [SerializeField, RichEnumExtends(typeof(ItemActionType))]
        RichEnumReference[] allowedActionType = {
            ItemActionType.Eat
        };

        public ItemTemplate[] SkinItems => _cachedSkinItems ?? ConvertSkinItems();
        ItemTemplate[] _cachedSkinItems;
        public ItemActionType[] AllowedActionType => _cachedAllowedActionType ?? ConvertAllowedActionTypes();
        ItemActionType[] _cachedAllowedActionType;
        
        public Element SpawnElement() => new UnlockItemSkin();
        public bool IsMine(Element element) => element is UnlockItemSkin;
        
        ItemActionType[] ConvertAllowedActionTypes() {
            ItemActionType[] types = new ItemActionType[allowedActionType.Length];
            for (int i = 0; i < allowedActionType.Length; i++) {
                types[i] = allowedActionType[i].EnumAs<ItemActionType>();
            }
            return types;
        }

        ItemTemplate[] ConvertSkinItems() {
            ItemTemplate[] items = new ItemTemplate[skinItems.Length];
            for (int i = 0; i < skinItems.Length; i++) {
                items[i] = skinItems[i].Get<ItemTemplate>();
            }

            return items;
        }
    }
}