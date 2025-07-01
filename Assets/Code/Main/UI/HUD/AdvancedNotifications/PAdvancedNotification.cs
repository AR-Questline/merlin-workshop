using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    public interface IPAdvancedNotification : IPresenter {
        void ForceShow();
    }
    
    public interface IPAdvancedNotification<TNotification> : IPAdvancedNotification where TNotification : class, IAdvancedNotification {
        void Show(TNotification notification);
    }
    
    public abstract class PAdvancedNotification<TNotification, TNotificationData> : Presenter<AdvancedNotificationBuffer>, IPAdvancedNotification<TNotification> where TNotification : class, IAdvancedNotification where TNotificationData : IPresenterNotificationData {
        bool _isOccupied;
        Sequence _sequence;
        
        protected abstract TNotificationData GetNotificationData();
        protected abstract NotificationSoundEvent GetNotificationSound(TNotification notification);
        
        protected TNotificationData Data { get; private set; }
        protected virtual bool IsIndependentUpdate => false;
        protected PresenterDataProvider PresenterDataProvider => Services.Get<PresenterDataProvider>();

        protected PAdvancedNotification(VisualElement parent) : base(parent) { }

        protected override void OnFullyInitialized() {
            Data = GetNotificationData();
            Content.Hide();
            Content.SetActiveOptimized(false);
            TryToInitializeAccessibilityBackground();
        }

        public void ForceShow() {
            Content.SetActiveOptimizedWithFullFade(true, 0.1f);
            if (this.Data is IPresenterNotificationDataWithHeight data) {
                Content.style.height = data.DefaultHeight;
            }
        }
        
        protected abstract Sequence ShowSequence();
        protected abstract void OnBeforeShow(TNotification notification);

        protected virtual void OnAfterHide() {
            ReleaseReleasable();
        }

        public void Show(TNotification notification) {
            if (_isOccupied) {
                Log.Minor?.Error("Presenter is already occupied by another notification. This should not happen!");
                _sequence.Kill();
            }
            
            _isOccupied = true;
            KeepIncomingNotificationLast();
            Content.SetActiveOptimized(true);
            PlaySound(notification, GetNotificationSound(notification)).Forget();
            OnBeforeShow(notification);
                
            // It might looks confusing why this way. It's because we can get a killed sequence by DOTween.Clear() when changing scenes and we want to 
            // clean presenter as we would complete it in the normal way. In addition onKill is triggered automatically when sequence is completed and we 
            // don't want to double clean the sequence when completion was successful.
            _sequence = ShowSequence()
                .OnComplete(() => ClearNotification(notification))
                .OnKill(() => ClearNotification(notification));
        }
        
        void TryToInitializeAccessibilityBackground() {
            if (this is IPresenterWithAccessibilityBackground bgOwner) {
                bgOwner.InitializeBackground(TargetModel);
            }
        }

        static async UniTaskVoid PlaySound(TNotification notification, NotificationSoundEvent notificationSound) {
            if (notificationSound.eventReference.IsNull) {
                return;
            }
            
            if(await AsyncUtil.WaitUntil(notification, () => Time.timeScale > 0)) {
                World.Services.Get<NotificationsAudioService>().PlayNotificationSound(notificationSound);
            }
        }

        void ClearNotification(TNotification notification) {
            if (notification.HasBeenDiscarded) {
                return;
            }
            
            _isOccupied = false;

            Content.SetActiveOptimized(false);
            Content.Hide();

            OnAfterHide();
            notification.Discard();
        }

        void KeepIncomingNotificationLast() {
            VisualElement parent = Content.parent;
            parent.Insert(parent.childCount - 1, Content);
        }

        protected override void DiscardInternal() {
            _sequence.Kill();
            Content?.RemoveFromHierarchy();
        }
    }
}