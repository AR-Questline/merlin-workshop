using System;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Pickables;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Location that can be picked up by Hero.")]
    public class PickItemAttachment : MonoBehaviour, IAttachmentSpec {
        [DetailedInfoBox(
@"PickItemAttachment is used for more advanced scenarios. See examples below:",
@"PickItemAttachment is used for more advanced scenarios. See examples below:
  - when you want to change its visibility
  - when you want to handle ItemPicked event in VisualScripting
  - when you want to reference it from story or other location
Otherwise, use PickableSpec instead."
        )]
        public bool destroyedAfterInteract = true;
        public ItemSpawningData itemReference;
        public bool triggerDialogueOnInteract;
        [ShowIf(nameof(triggerDialogueOnInteract))] public StoryBookmark storyBookmark;
        
        public Element SpawnElement() {
            return new PickItemAction(itemReference.ToRuntimeData(this), destroyedAfterInteract);
        }
        public bool IsMine(Element element) => element is PickItemAction;

        public bool SetItemReferenceIfNull(ItemSpawningData dataToSet) {
            if (itemReference != null) return false;
            
            itemReference = dataToSet;
            return true;
        }

        [Button, HideInPlayMode]
        void ConvertToPickableSpec() {
            var go = gameObject;
            ConvertToPickableSpec(true, true, out bool removeFromAddressable);
            if (removeFromAddressable) {
                Log.Important?.Error("You need to remove this asset from addressable manually!", go);
            }
        }

        public void ConvertToPickableSpec(bool allowDestroyingAssets, bool convertTemplates, out bool removeFromAddressable) {
            removeFromAddressable = false;
            
            try {
                if (TryGetComponent(out LocationTemplate template)) {
                    if (!convertTemplates) {
                        Log.Important?.Error(
                            $"PickItemAttachment <color=blue>{name}</color> has LocationTemplate! " +
                            "If you really want to convert it, do it manually. " +
                            "Remember to <b>remove it from addressables</b>.", 
                            gameObject
                        );
                        return;
                    }
                    DestroyImmediate(template, allowDestroyingAssets);
                    removeFromAddressable = true;
                }
                
                var pickableSpec = gameObject.AddComponent<PickableSpec>();
                pickableSpec.Setup(itemReference.itemTemplateReference, itemReference.quantity);

                DestroyImmediate(GetComponent<LocationSpec>(), allowDestroyingAssets);
                foreach (Transform child in transform) {
                    DestroyImmediate(child.gameObject, allowDestroyingAssets);
                }
                DestroyImmediate(this, allowDestroyingAssets);
            } catch (Exception e) {
                Log.Important?.Error($"Failed to convert to PickableSpec on scene {gameObject.scene}. See exception below.", gameObject);
                Debug.LogException(e);
            }
        }
    }
}