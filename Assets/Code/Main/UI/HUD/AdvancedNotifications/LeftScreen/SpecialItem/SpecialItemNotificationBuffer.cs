using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Shops.UI;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.SpecialItem {
    public partial class SpecialItemNotificationBuffer : AdvancedNotificationBufferPresenter<SpecialItemNotification> {
        protected override bool HideWhenMapNotInteractive => !World.HasAny<Story>() || World.HasAny<ShopUI>();

        static Func<Heroes.Items.Item, bool> IsKeyItem => item => TagUtils.HasRequiredTag(item.Tags, "item:quest") ||
                                                                  item.Quality == ItemQuality.Quest ||
                                                                  TagUtils.HasRequiredTag(item.Tags, "item:important") ||
                                                                  (item.Quality == ItemQuality.Magic && !Hero.Current.HeroItems.IsKnownItem(item.Template));
        static HeroReadables ReadByHero => Hero.Current.Element<HeroReadables>();
        static bool ReadableShouldBeNotified(Heroes.Items.Item item) => item.IsReadable && !ReadByHero.WasTemplateRead(item.Template);
        static Func<Heroes.Items.Item, bool> IsItemInTemporaryStash => item => World.Any<HeroItemsTemporaryStash>()?.ContainsItem(item) ?? false;

        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.SpecialItemNotificationsParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(ProficiencyNotificationBuffer);
            }
        }

        public void TryToPush(Heroes.Items.Item item) {
            if (item.HiddenOnUI || IsItemInTemporaryStash(item)) {
                return;
            }

            if (ReadableShouldBeNotified(item) || IsKeyItem(item)) {
                NotificationUtils.Push(new SpecialItemNotification(item));
            }
        }

        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.specialItemNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<SpecialItemNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PSpecialItemNotification pSpecialItemNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pSpecialItemNotification);
        }

        protected override void MergeSimilarNotifications(SpecialItemNotification notification) {
            ItemTemplate template = notification.item.Template;
            int queueCount = notificationQueue.Count;
            for (int i = 0; i < queueCount; i++) {
                var queuedItemNotification = notificationQueue.Dequeue();
                if (queuedItemNotification.item.Template == template) {
                    queuedItemNotification.Discard();
                } else {
                    notificationQueue.Enqueue(queuedItemNotification);
                }
            }
        }
    }
}