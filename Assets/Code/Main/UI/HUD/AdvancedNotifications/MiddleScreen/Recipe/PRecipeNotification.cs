using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.Utility;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Recipe {
    public class PRecipeNotification : PAdvancedNotification<RecipeNotification, PRecipeNotificationData> {
        BetterOutlinedLabel _recipeName;
        BetterOutlinedLabel _recipeDescription;
        VisualElement _recipeIcon;
        
        public PRecipeNotification(VisualElement parent) : base(parent) { }
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _recipeName = contentRoot.Q<BetterOutlinedLabel>("recipe-name");
            _recipeDescription = contentRoot.Q<BetterOutlinedLabel>("recipe-description");
            _recipeIcon = contentRoot.Q<VisualElement>("recipe-icon");
        }

        protected override PRecipeNotificationData GetNotificationData() {
            return PresenterDataProvider.recipeNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(RecipeNotification notification) {
            return CommonReferences.Get.AudioConfig.RecipeAudio;
        }

        protected override void OnBeforeShow(RecipeNotification notification) {
            _recipeName.text = notification.RecipeName;
            _recipeDescription.text = notification.RecipeDescription.LineHeight(90);
            
            if (notification.IconReference is {IsSet: true} iconRef) {
                iconRef.RegisterAndSetup(this, _recipeIcon);
            }
        }

        protected override Sequence ShowSequence() {
            Content.style.height = 0f;
            
            return DOTween.Sequence().SetUpdate(IsIndependentUpdate)
                .Append(Content.DoFade(1f, Data.FadeDuration))
                .Join(Content.DoHeight(Data.DefaultHeight, Data.HeightDuration))
                .AppendInterval(Data.VisibilityDuration)
                .Append(Content.DoFade(0f, Data.FadeDuration))
                .Append(Content.DoHeight(0f, Data.HeightDuration));
        }
    }
}