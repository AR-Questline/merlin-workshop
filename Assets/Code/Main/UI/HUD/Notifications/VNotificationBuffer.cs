using System;
using System.Collections.Generic;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Extensions;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.Notifications {
    [UsesPrefab("HUD/Notifications/VNotificationBuffer")]
    public class VNotificationBuffer : View<NotificationBuffer> {
        static readonly List<VNotification> NotificationsBuffer = new List<VNotification>(8);

        // === Inspector Properties
        public float acceleration;
        public float drag;
        public Transform notificationParent;
        public CanvasGroup canvasGroup;

        // offset from first notification to the container's pivot
        float _offset;
        float _offsetVelocity;
        List<RectTransform> notificationRects = new List<RectTransform>();

        Action _waitingForVisible;
        bool _isHidden;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD();

        protected override void OnInitialize() {
            // detect the notifications are added/removed
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<Notification>(), Target,
                OnNotificationAdded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<Notification>(), Target,
                OnNotificationRemoved);

            // UI state listener
            World.Any<UIStateStack>()?.ListenTo(UIStateStack.Events.UIStateChanged, UpdateInteractivity, this);
            // try-catch to avoid corrupted save when DOTween crashes 
            try {
                UpdateInteractivity(World.Any<UIStateStack>()?.State ?? UIState.BaseState);
            } catch (IndexOutOfRangeException e) {
                Debug.LogException(e);
            }
        }

        void Update() {
            if (!_isHidden) {
                Target.ClearBuffer();
            }

            if (_offset >= 0f) {
                _offset = 0f;
                _offsetVelocity = 0f;
            } else {
                _offsetVelocity = Mathf.Min(_offsetVelocity + acceleration * Time.deltaTime, -_offset * drag);
                _offset = Mathf.Min(0f, _offset + _offsetVelocity);
            }
            UpdateViews();
        }

        // === Helpers
        void OnNotificationAdded(Model model) {
            if (model is Notification notification) {
                notificationRects.Add(notification.MainView.transform as RectTransform);
                UpdateViews();
            }
        }

        void OnNotificationRemoved(Model model) {
            if (model is Notification) {
                notificationRects.RemoveAt(0);
                _offset = notificationRects.Count > 0 ? notificationRects[0].anchoredPosition.y : 0f;
            }
        }

        void UpdateViews() {
            var alt = _offset;
            notificationRects.RemoveAll(r => r == null);
            foreach (RectTransform rect in notificationRects) {
                var position = rect.anchoredPosition;
                position.y = alt;
                rect.anchoredPosition = position;
                alt -= rect.sizeDelta.y;
            }
        }

        public void WhenVisible(Action call) {
            if (call == null) return;
            if (_isHidden) {
                _waitingForVisible += call;
            } else {
                call();
            }
        }

        void UpdateInteractivity(UIState state) {
            _isHidden = state.HudState.HasFlagFast(HUDState.NotificationsHidden);

            if (_isHidden) {
                DOTween.To(() => canvasGroup.alpha, v => canvasGroup.alpha = v, 0f, 0.3f);
                SetNotificationsAnimationSpeed(0f);
            } else {
                DOTween.To(() => canvasGroup.alpha, v => canvasGroup.alpha = v, 1f, 0.3f);
                SetNotificationsAnimationSpeed(1f);
                _waitingForVisible?.Invoke();
                _waitingForVisible = null;
            }
        }

        void SetNotificationsAnimationSpeed(float speed) {
            GetComponentsInChildren(NotificationsBuffer);
            foreach (var notification in NotificationsBuffer) {
                notification.SetAnimatorSpeed(speed);
            }
            NotificationsBuffer.Clear();
        }
    }
}