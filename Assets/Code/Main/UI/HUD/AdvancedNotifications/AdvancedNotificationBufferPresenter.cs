using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    /// <summary>
    /// Marker for QC Debug Tools
    /// </summary>
    public interface IAdvancedNotificationBufferPresenter : IModel {
        void ForceDisplayingNotifications();
    }
    
    public abstract partial class AdvancedNotificationBufferPresenter<TNotification> : AdvancedNotificationBuffer<TNotification>, IAdvancedNotificationBufferPresenter where TNotification : AdvancedNotification {
        protected override bool HideWhenMapNotInteractive => true;
        ARAssetReference _notificationPrototypeReference;
        readonly Queue<IPAdvancedNotification<TNotification>> _notificationPresenters = new();
        
        public Func<TNotification, bool> FilterNotification { get; private set; }
        protected static PresenterDataProvider PresenterDataProvider => Services.Get<PresenterDataProvider>();
        
        protected override void OnFullyInitialized() {
            var uxml = RetrieveNotificationBaseData().uxml;
            if (uxml is {IsSet: true}) {
                _notificationPrototypeReference = uxml.GetAndLoad<VisualTreeAsset>(handle => PrewarmBuffer(handle.Result));
            }
        }

        public void ForceDisplayingNotifications() {
            World.EventSystem.RemoveListener(_stateStackListener);
            
            NotificationsParent.SetActiveOptimizedWithFullFade(true, 0.1f);
            foreach (var presenter in Presenters) {
                IPAdvancedNotification notification = presenter as IPAdvancedNotification;
                notification?.ForceShow();
            }
        }
        
        public void SetNotificationFilter(Func<TNotification, bool> filter) {
            FilterNotification = filter;
        }

        public void ClearNotificationFilter() {
            FilterNotification = null;
        }

        protected abstract PBaseData RetrieveNotificationBaseData();
        protected abstract IPAdvancedNotification<TNotification> MakeNotificationPresenter(VisualTreeAsset prototype);

        protected override void ShowPresenterNotification(TNotification notification) {
            if (_notificationPresenters.TryDequeue(out var freePresenter)) {
                _notificationPresenters.Enqueue(freePresenter);
                freePresenter.Show(notification);
            }
        }
        
        void PrewarmBuffer(VisualTreeAsset prototype) {
            for (int i = 0; i < MaxVisibleNotifications; i++) {
                var presenter = MakeNotificationPresenter(prototype);
                _notificationPresenters.Enqueue(presenter);
                NotificationsParent.Add(presenter.Content);
            }
            
            IsReady = true;
            TryToPush();
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            _notificationPrototypeReference?.ReleaseAsset();
            base.OnDiscard(fromDomainDrop);
        }
    }
}