using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemIconComponent : ItemSlotComponent {
        [SerializeField] Image icon;
        [SerializeField] GameObject lockIcon;
        
        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            if (lockIcon != null) {
                lockIcon.SetActive(item.Locked);
            }
            
            if (item.Icon?.IsSet ?? false) {
                try {
                    item.Icon.RegisterAndSetup(view, icon);
                } catch (Exception e) {
                    Log.Important?.Error($"Exception below happened while trying to load item icon! Item: {item.Template}");
                    Debug.LogException(e);
                    return;
                }

                SetInternalVisibility(true);
            } else {
                SetInternalVisibility(false);
            }
        }

        public void SetMaterial(Material material) {
            icon.material = material;
        }
    }
}