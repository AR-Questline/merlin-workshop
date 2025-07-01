using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.CustomControls;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.Utility;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item {
    public class PItemNotification : PAdvancedNotification<ItemNotification, PItemNotificationData>, IPresenterWithAccessibilityBackground {
        BetterOutlinedLabel _itemName;
        VisualItemIcon _itemIcon;
        
        VisualElement IPresenterWithAccessibilityBackground.Host => Content;

        public PItemNotification(VisualElement parent) : base(parent) { }
        
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _itemName = contentRoot.Q<BetterOutlinedLabel>("item-name");
            _itemIcon = new VisualItemIcon(contentRoot.Q<VisualElement>("item-icon"));
            
            Content.style.height = 0f;
        }

        protected override PItemNotificationData GetNotificationData() {
            return PresenterDataProvider.itemNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(ItemNotification notification) {
            return CommonReferences.Get.AudioConfig.ItemAudio;
        }

        protected override void OnBeforeShow(ItemNotification notification) {
            var itemData = notification.itemData;
            
            if (itemData.quantity == null) {
                _itemName.text = itemData.itemName;
            } else switch (itemData.changeSign) {
                case 'x':
                    _itemName.text = LocTerms.ItemWithQuantity.Translate(itemData.quantity.Value, itemData.itemName);
                    break;
                case '+':
                    _itemName.text = LocTerms.ItemWithPositiveQuantityChange.Translate(itemData.quantity.Value, itemData.itemName);
                    break;
                case '-':
                    _itemName.text = LocTerms.ItemWithNegativeQuantityChange.Translate(itemData.quantity.Value, itemData.itemName);
                    break;
                default: {
                    _itemName.text = $"{itemData.itemName} {itemData.changeSign}{itemData.quantity.Value}";
                    break;
                }
            }
            
            _itemName.SetTextColor(itemData.color);
            
            if (notification.itemData.itemIcon is {IsSet: true} icon) {
                _itemIcon.Content.SetActiveOptimized(true);
                _itemIcon.Set(icon, this);
            } else {
                _itemIcon.Content.SetActiveOptimized(false);
            }
        }

        protected override Sequence ShowSequence() {
            Content.transform.position = Vector3.right * Data.InitialXOffset;
            Content.style.height = 0f;
            
            float fadeDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastFade : Data.FadeDuration;
            float moveDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastMove : Data.MoveDuration;
            float heightDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastHeight : Data.HeightDuration;
            float visibilityDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastVisibility : Data.VisibilityDuration;
            
            return DOTween.Sequence().SetUpdate(IsIndependentUpdate)
                .Append(Content.DoFade(1f, fadeDuration))
                .Join(Content.DoMove(Vector3.zero, moveDuration))
                .Join(Content.DoHeight(Data.DefaultHeight, heightDuration))
                .AppendInterval(visibilityDuration)
                .Append(Content.DoFade(0f, fadeDuration))
            .Append(Content.DoHeight(0f, heightDuration));
        }
    }
}