using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item {
    public partial class ItemNotificationBuffer : AdvancedNotificationBufferPresenter<ItemNotification> {
        const int MaxVisibleNotificationsCount = 5;
        int _maxVisibleNotifications;

        protected override bool HideWhenMapNotInteractive => !World.HasAny<Story>();
        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.ItemNotificationsParent;
        protected override int MaxVisibleNotifications => _maxVisibleNotifications;

        protected override void OnInitialize() {
            _maxVisibleNotifications = MaxVisibleNotificationsCount;
            base.OnInitialize();
            World.EventSystem.ListenTo(EventSelector.AnySource, Stat.Events.StatChangedBy(CurrencyStatType.Wealth), this, OnWealthChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, Stat.Events.StatChangedBy(CurrencyStatType.Cobweb), this, OnCobwebChanged);
        }
        
        public void ChangeVisualElementParentStyle(string oldParentClass, string newParentClass) {
            NotificationsParent.RemoveFromClassList(oldParentClass);
            NotificationsParent.AddToClassList(newParentClass);
        }
        
        public void SetMaxVisibleNotifications(int maxVisibleNotifications) {
            _maxVisibleNotifications = maxVisibleNotifications;
        }

        public void ResetMaxVisibleNotifications() {
            _maxVisibleNotifications = MaxVisibleNotificationsCount;
        }

        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.itemNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<ItemNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PItemNotification pItemNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pItemNotification);
        }

        protected override void MergeSimilarNotifications(ItemNotification notification) {
            ItemData itemData = notification.itemData;
            int totalQuantity = itemData.quantity;
            int queueCount = notificationQueue.Count;
            for (int i = 0; i < queueCount; i++) {
                var queuedItemNotification = notificationQueue.Dequeue();
                if (queuedItemNotification.itemData.itemName == itemData.itemName) {
                    totalQuantity += queuedItemNotification.itemData.quantity;
                    queuedItemNotification.Discard();
                } else {
                    notificationQueue.Enqueue(queuedItemNotification);
                }
            }

            notification.OverrideItemData(itemData.itemTemplate == null
                ? new ItemData(itemData.itemName, totalQuantity)
                : new ItemData(itemData.itemTemplate, totalQuantity));
        }

        static void OnWealthChanged(Stat.StatChange statChange) {
            if (statChange.stat.Owner is Hero hero) {
                ItemUtils.AnnounceGettingItem(CommonReferences.Get.CoinItemTemplate, (int) statChange.value);
            }
        }

        static void OnCobwebChanged(Stat.StatChange statChange) {
            if (statChange.stat.Owner is Hero hero) {
                ItemUtils.AnnounceGettingItem(CommonReferences.Get.CobwebItemTemplate, (int) statChange.value);
            }
        }
    }
}