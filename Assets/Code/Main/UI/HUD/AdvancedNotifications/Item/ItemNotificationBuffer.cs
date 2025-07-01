using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item {
    public partial class ItemNotificationBuffer : AdvancedNotificationBuffer<ItemNotification> {
        public sealed override bool IsNotSaved => true;

        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.ItemNotificationsParent;
        protected override int MaxVisibleNotifications => 5;

        protected override void OnInitialize() {
            base.OnInitialize();
            World.EventSystem.ListenTo(EventSelector.AnySource, Stat.Events.StatChangedBy(CurrencyStatType.Wealth), this, OnWealthChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, Stat.Events.StatChangedBy(CurrencyStatType.Cobweb), this, OnCobwebChanged);
        }

        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.itemNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<ItemNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PItemNotification pItemNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pItemNotification);
        }

        static void OnWealthChanged(Stat.StatChange statChange) {
            if (statChange.stat.Owner is Hero hero) {
                ItemUtils.AnnounceGettingItem(CommonReferences.Get.CoinItemTemplate, (int) statChange.value, hero);
            }
        }

        static void OnCobwebChanged(Stat.StatChange statChange) {
            if (statChange.stat.Owner is Hero hero) {
                ItemUtils.AnnounceGettingItem(CommonReferences.Get.CobwebItemTemplate, (int) statChange.value, hero);
            }
        }
    }
}