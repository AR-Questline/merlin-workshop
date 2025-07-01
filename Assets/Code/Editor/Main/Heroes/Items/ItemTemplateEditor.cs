using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Main.Templates;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Main.Heroes.Items;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Heroes.Items {
    [CustomEditor(typeof(ItemTemplate)), CanEditMultipleObjects]
    public class ItemTemplateEditor : TemplateEditor {
        public static readonly HashSet<string> Labels = new(new []{ "Item", "Icon", "Sprite" });

        ItemTemplate _item;
        string _assetGUID;

        protected override void OnEnable() {
            base.OnEnable();
            _item  = (ItemTemplate) target;
            if (_item.iconReference.IsSet) {
                _assetGUID = _item.iconReference.arSpriteReference.Address;
                AddressableHelper.EnsureAsset(_item.iconReference.arSpriteReference.Address, ValidGroup, ValidAddress, ValidLabels);
            }
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (_item.iconReference.IsSet && _assetGUID != _item.iconReference.arSpriteReference.Address) {
                AddressableHelper.EnsureAsset( _item.iconReference.arSpriteReference.Address, ValidGroup, ValidAddress, ValidLabels);
            }

            if (GUILayout.Button("Show All Loot Occurrences")) {
                LootSearchWindow.OpenWindowOn((ItemTemplate)target);
            }
        }

        string ValidGroup(Object icon, AddressableAssetEntry entry, string oldGroup) {
            return AddressableGroup.ItemsIcons.NameOf();
        }

        string ValidAddress(Object icon, AddressableAssetEntry entry) {
            return GetIconAddressName(icon);
        }

        HashSet<string> ValidLabels(Object icon, AddressableAssetEntry entry) {
            var labels = entry.labels;
            return labels.Intersect(Labels).Count() == Labels.Count ? labels : Labels;
        }

        public static string GetIconAddressName(Object icon) {
            return $"Icon/{icon.name}";
        }
    }
}