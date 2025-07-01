using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Tutorials.TutorialPrompts;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Recipe;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Views {
    [UsesPrefab("UI/Tutorials/VTutorialHUD")]
    public class VTutorialHUD : View<HUD> {
        const float FadeInTime = 0.25f;
        const float FadeOutTime = 0.75f;
        
        [SerializeField] Transform promptsParent;
        [SerializeField, LocStringCategory(Category.UI)] LocString title;
        [SerializeField] TextMeshProUGUI titleHolder;
        [SerializeField] CanvasGroup content;
        
        Tween _visibilityTween;
        UIStateStack _stateStack;
        RecipeNotificationBuffer _recipeNotificationBuffer;
        
        public Transform PromptsParent => promptsParent;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD();

        protected override void OnInitialize() {
            content.alpha = 0;
            content.TrySetActiveOptimized(false);
            
            _stateStack = UIStateStack.Instance;
            _stateStack.ListenTo(UIStateStack.Events.UIStateChanged, Refresh, this);
            titleHolder.text = title.ToString();
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);

            ModelUtils.DoForFirstModelOfType<RecipeNotificationBuffer>(recipeNotificationBuffer => {
                _recipeNotificationBuffer = recipeNotificationBuffer;
                _recipeNotificationBuffer.ListenTo(AdvancedNotificationBuffer.Events.AfterPushingNewNotification, Refresh, this);
                _recipeNotificationBuffer.ListenTo(AdvancedNotificationBuffer.Events.AfterPushingLastNotification, Refresh, this);
            }, this);
        }

        void Refresh() {
            bool shouldBeVisible = !_stateStack.State.HudState.HasFlagFast(HUDState.TutorialsHidden) && _stateStack.State.IsMapInteractive && Target.HasElement<TutorialPrompt>() && !_recipeNotificationBuffer.IsPushing;

            content.TrySetActiveOptimized(true);
            _visibilityTween.Kill();
            _visibilityTween = content.DOFade(shouldBeVisible ? 1 : 0, shouldBeVisible ? FadeInTime : FadeOutTime).SetUpdate(true).OnComplete(() => {;
                if (!shouldBeVisible) {
                    content.TrySetActiveOptimized(false);
                }
            });
        }
    }
}
