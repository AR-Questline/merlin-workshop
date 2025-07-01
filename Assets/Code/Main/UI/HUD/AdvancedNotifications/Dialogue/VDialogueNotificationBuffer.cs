using Awaken.TG.Graphics.Transitions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Dialogue {
    [UsesPrefab("HUD/AdvancedNotifications/VDialogueNotificationBuffer")]
    public class VDialogueNotificationBuffer : VAdvancedNotificationBuffer {
        const int SortingOrderWhenInTransition = 99;
        
        [SerializeField] RectTransform bufferArea;
        [SerializeField] LayoutElement scrollLayoutElement;
        [SerializeField, Range(0.00001f,4*45)] float tweenSpeed = 100f;
        [Space]
        [SerializeField, ReadOnly] float targetHeight = 0;

        float ContentHeight => NotificationParent.sizeDelta.y;

        TweenerCore<float, float, FloatOptions> _tween;
        Canvas _canvas;
        int _defaultSortingOrder;

        protected override void OnInitialize() {
            base.OnInitialize();
            Target.ListenTo(AdvancedNotificationBuffer.Events.AfterPushingNewNotification, n => AfterNewNotificationPushed(n).Forget(), this);
            SetScrollerHeight(bufferArea.sizeDelta.y);
            targetHeight = bufferArea.sizeDelta.y;
            _canvas = GetComponent<Canvas>();
            _defaultSortingOrder = _canvas.sortingOrder;
        }

        async UniTaskVoid AfterNewNotificationPushed(IAdvancedNotification notification) {
            UpdateSortingOrder();
            notification.ListenTo(Model.Events.BeforeDiscarded, BeforeNotificationDiscarded, this);

            await UniTask.WaitForEndOfFrame(this);
            _tween.Kill();
            targetHeight = bufferArea.sizeDelta.y - ContentHeight;
            LaunchTween();
        }

        void LaunchTween() {
            _tween = DOTween.To(() => scrollLayoutElement.minHeight, SetScrollerHeight, targetHeight,  Mathf.Abs(targetHeight - scrollLayoutElement.minHeight)/tweenSpeed).SetUpdate(true);
            _tween.Play();
        }

        void BeforeNotificationDiscarded(IModel notification) {
            _tween.Kill();
            var rect = (RectTransform)notification.View<VDialogueNotification>().transform;
            Vector2 rectSizeDelta = rect.sizeDelta;
            
            SetScrollerHeight(scrollLayoutElement.minHeight + rectSizeDelta.y);
            targetHeight = bufferArea.sizeDelta.y - (ContentHeight - rectSizeDelta.y); // Adjust for content height not having updated yet
            LaunchTween();

            UpdateSortingOrder();
        }

        void SetScrollerHeight(float targetVal) {
            scrollLayoutElement.minHeight = targetVal;
        }

        void UpdateSortingOrder() {
            bool inTransition = Services.TryGet<TransitionService>()?.InTransition ?? false;
            _canvas.sortingOrder = inTransition ? SortingOrderWhenInTransition : _defaultSortingOrder;
        }
    }
}