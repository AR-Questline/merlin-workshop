using System;
using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.SpecialItem {
    public partial class SpecialItemNotificationBuffer : AdvancedNotificationBuffer<SpecialItemNotification> {
        public sealed override bool IsNotSaved => true;

        static Func<Heroes.Items.Item, bool> IsKeyItem => item => TagUtils.HasRequiredTag(item.Tags, "item:quest") || TagUtils.HasRequiredTag(item.Tags, "item:important");
        static HeroReadables ReadByHero => Hero.Current.Element<HeroReadables>();
        static bool ReadableShouldBeNotified(Heroes.Items.Item item) => item.IsReadable && !ReadByHero.WasTemplateRead(item.Template);
        static Func<Heroes.Items.Item, bool> IsItemInTemporaryStash => item => World.Any<HeroItemsTemporaryStash>()?.ContainsItem(item) ?? false;

        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.SpecialItemNotificationsParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(ProficiencyNotificationBuffer);
            }
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            ModelUtils.DoForFirstModelOfType<Hero>(AddListeners,this);
        }

        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.specialItemNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<SpecialItemNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PSpecialItemNotification pSpecialItemNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pSpecialItemNotification);
        }
        
        void AddListeners(Hero h) {
            h.Inventory.ListenTo(ICharacterInventory.Events.PickedUpNewItem, OnItemAcquired, this);
        }
        
        void OnItemAcquired(Heroes.Items.Item item) {
            if (item.HiddenOnUI || IsItemInTemporaryStash(item)) {
                return;
            }

            if (ReadableShouldBeNotified(item) || IsKeyItem(item)) {
                AdvancedNotificationBuffer.Push<SpecialItemNotificationBuffer>(new SpecialItemNotification(item));
            }
        }
    }
}