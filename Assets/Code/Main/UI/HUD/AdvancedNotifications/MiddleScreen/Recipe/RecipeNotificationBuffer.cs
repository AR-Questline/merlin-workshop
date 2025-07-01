using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using DG.Tweening;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Recipe {
    public partial class RecipeNotificationBuffer : AdvancedNotificationBuffer<RecipeNotification> {
        const float FadeDuration = 0.3f;
        const float DelayDuration = 4.7f;

        public sealed override bool IsNotSaved => true;
        
        Tween _fadeTween;
        
        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.RecipeNotificationsParent;
        protected override int MaxVisibleNotifications => 4;
        protected override bool HideWhenMapNotInteractive => true;

        protected override void OnAfterPushingNewNotification() {
            _fadeTween.Kill();
            _fadeTween = NotificationsParent.DoFade(1f, FadeDuration).SetUpdate(true);
        }

        protected override void OnBeforePushingLastNotification() {
            _fadeTween?.Kill(true);
            _fadeTween = NotificationsParent.DoFade(0f, FadeDuration).SetDelay(DelayDuration).SetUpdate(true);
        }

        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.recipeNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<RecipeNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PRecipeNotification pRecipeNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pRecipeNotification);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            UITweens.DiscardTween(ref _fadeTween);
        }
    }
}