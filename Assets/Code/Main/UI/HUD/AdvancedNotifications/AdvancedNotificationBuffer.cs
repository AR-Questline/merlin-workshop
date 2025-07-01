using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.BufferBlockers;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    public abstract partial class AdvancedNotificationBuffer : Element<HUD> {
        readonly Queue<IAdvancedNotification> _notificationQueue = new();
        
        int _shownCounter;
        UIStateStack _stateStack;
        bool _forceVisible;
        bool _suspendPushingNotifications;
        HudBackgroundsIntensity _hudBackgroundsIntensity;
        IEventListener _bufferBlockerListener;
        IEventListener _dependentBufferListener;
        
        protected IEventListener _stateStackListener;
        
        public bool IsPushing { get; private set; }

        protected bool IsReady { get; set; }
        protected virtual bool HideWhenMapNotInteractive => false;
        protected virtual int MaxVisibleNotifications => 1;
        protected virtual bool StrictMaxVisibleNotifications => false;
        protected virtual VisualElement NotificationsParent => null;
        
        CanvasGroup BufferCanvasGroup => View<VAdvancedNotificationBuffer>()?.BufferCanvasGroup;
        bool ShouldBeHidden => !_forceVisible && HideWhenMapNotInteractive && !_stateStack.State.IsMapInteractive;
        
        /// <summary>
        /// Use DependentBuffers to specify which buffers should be checked before pushing a new notification.
        /// Using Dependent Buffers you have to be more specific what types to check. If you want to block some
        /// buffers together, and don't care about the type of buffer you can use BufferBlocker.cs
        /// </summary>
        protected virtual IEnumerable<Type> DependentBuffers { get; [UnityEngine.Scripting.Preserve] set; }

        public virtual bool SuspendPushingNotifications {
            get => _suspendPushingNotifications || AllNotificationsSuspended;
            set => _suspendPushingNotifications = value;
        }
        
        public static bool AllNotificationsSuspended { get; set; }

        public new static class Events {
            public static readonly Event<AdvancedNotificationBuffer, bool> BeforePushingFirstNotification = new(nameof(BeforePushingFirstNotification));
            public static readonly Event<AdvancedNotificationBuffer, bool> AfterPushingLastNotification = new(nameof(AfterPushingLastNotification));
            public static readonly Event<AdvancedNotificationBuffer, IAdvancedNotification> AfterPushingNewNotification = new(nameof(AfterPushingNewNotification));
        }

        protected override void OnInitialize() {
            _stateStack = UIStateStack.Instance;
            _stateStackListener = _stateStack.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
        }

        protected override void OnFullyInitialized() {
            IsReady = true;
            TryToPush();
        }

        public static void Push<TBuffer>(IAdvancedNotification notification) where TBuffer : AdvancedNotificationBuffer {
            World.Only<TBuffer>().PushNotification(notification);
        }
        
        protected virtual void OnBeforePushingFirstNotification() { }
        protected virtual void OnBeforePushingLastNotification() { }
        protected virtual void OnAfterPushingLastNotification() { }
        protected virtual void OnAfterPushingNewNotification() { }

        protected void TryToPush() {
            if (!IsReady || HasBeenDiscarded) return;
            
            if (_shownCounter >= MaxVisibleNotifications) return;

            IAdvancedBufferWithBlocker bufferWithBlocker = this as IAdvancedBufferWithBlocker;
            if (!IsPushing && bufferWithBlocker != null) {
                if (_bufferBlockerListener != null) {
                    return;
                }
                
                bufferWithBlocker = (IAdvancedBufferWithBlocker) this;
                var externalBufferBlocker = World.All<BufferBlocker>(bufferWithBlocker.BlockerType).FirstOrDefault(b => b.ParentModel != this);
                if (externalBufferBlocker != null) {
                    _bufferBlockerListener = externalBufferBlocker.ListenToLimited(Model.Events.AfterDiscarded, OnExternalBufferBlockerDiscarded, this);
                    return;
                }
            }

            // resolve dependent buffers
            bool hasDependentBuffers = DependentBuffers != null && DependentBuffers.Any();
            if (hasDependentBuffers) {
                if (_dependentBufferListener != null) {
                    return;
                }
                
                foreach (Type dependentBuffer in DependentBuffers) {
                    var buffer = World.All<AdvancedNotificationBuffer>(dependentBuffer).FirstOrDefault(dp => dp.IsPushing);
                    if (buffer != null) {
                        _dependentBufferListener = buffer.ListenToLimited(Events.AfterPushingLastNotification, OnDependentBufferStoppedPushing, this);
                        return;
                    }
                }
            }

            if (ShouldBeHidden) {
                return;
            }
            
            while (true) {
                if (_notificationQueue.TryDequeue(out IAdvancedNotification notification)) {
                    if (notification.HasBeenDiscarded) {
                        continue;
                    }

                    if (!notification.IsValid) {
                        notification.Discard();
                        continue;
                    }
                    
                    if (_shownCounter == 0 && !IsPushing) {
                        bufferWithBlocker?.AddBlockerForAnotherBuffers();
                        SetBufferCanvasGroupAlpha(1f);
                        this.Trigger(Events.BeforePushingFirstNotification, true);
                        _hudBackgroundsIntensity ??= World.Only<HudBackgroundsIntensity>();
                        OnBeforePushingFirstNotification();
                    }

                    IsPushing = true;
                    ShowNotification(notification);
                } else {
                    if (_shownCounter <= 0) {
                        IsPushing = false;
                        this.Trigger(Events.AfterPushingLastNotification, true);
                        OnAfterPushingLastNotification();
                        TryGetElement<BufferBlocker>()?.Discard();
                    }
                }

                break;
            }
        }
        
        void OnExternalBufferBlockerDiscarded() {
            _bufferBlockerListener = null;
            TryToPush();
        }
        
        void OnDependentBufferStoppedPushing() {
            _dependentBufferListener = null;
            TryToPush();
        }

        void OnUIStateChanged(UIState state) {
            if (ShouldBeHidden && IsPushing) {
                SetBufferCanvasGroupAlpha(0f);
                return;
            }
            
            SetBufferCanvasGroupAlpha(IsPushing ? 1f : 0f);
            int minToPush = Mathf.Min(_notificationQueue.Count, MaxVisibleNotifications);
            if (!ShouldBeHidden && minToPush > 0) {
                for (var i = 0; i < minToPush; i++) {
                    TryToPush();
                }
            }

            if (!ShouldBeHidden && _shownCounter == 0 && _notificationQueue.Count == 0 && IsPushing) {
                SetBufferCanvasGroupAlpha(0f);
                TryToPush();
            }
        }

        void OnVisibleDiscard(IModel discarded) {
            _shownCounter--;
            TryToPush();
        }

        void ShowNotification(IAdvancedNotification notification) {
            notification.Show(); //old way of showing notifications - to be removed when all notifications are converted to UIToolkit
            ShowPresenterNotification(notification); //new way of showing notifications with UIToolkit
            _shownCounter++;
            notification.ListenTo(Model.Events.AfterDiscarded, OnVisibleDiscard, this);
            this.Trigger(Events.AfterPushingNewNotification, notification);
            OnAfterPushingNewNotification();

            if (_notificationQueue.Count == 0) {
                OnBeforePushingLastNotification();
            }
        }

        void SetBufferCanvasGroupAlpha(float alpha) {
            if (BufferCanvasGroup != null) {
                BufferCanvasGroup.alpha = alpha;
            }

            NotificationsParent?.SetActiveOptimized(alpha > 0f);
        }
        
        public void ClearBuffer() {
            var notifications = Elements<IAdvancedNotification>();
            foreach (var notification in notifications) {
                notification.Discard();
            }
            
            _notificationQueue.Clear();
        }

        public void PushNotification(IAdvancedNotification notificationElement) {
            if (SuspendPushingNotifications || notificationElement == null) {
                return;
            }
            
            AddElement(notificationElement);
            _notificationQueue.Enqueue(notificationElement);

            var notifications = Elements<IAdvancedNotification>();
            if (notifications.CountGreaterThan(MaxVisibleNotifications) && StrictMaxVisibleNotifications) {
                notifications.First().Discard();
            } else {
                TryToPush();
            }
        }
        
        public void ChangeForceVisible(bool value) {
            if (_forceVisible == value) {
                return;
            }
            _forceVisible = value;
            OnUIStateChanged(_stateStack.State);
        }

        protected virtual void ShowPresenterNotification(IAdvancedNotification notification) { }

        public class SuspendNotifications<T> : IDisposable where T : AdvancedNotificationBuffer {
            readonly AdvancedNotificationBuffer _buffer;
            readonly bool _previousState;
            
            public SuspendNotifications() {
                _buffer = World.Any<T>();
                _previousState = _buffer.SuspendPushingNotifications;
                _buffer.SuspendPushingNotifications = true;
            }
            
            public void Dispose() {
                _buffer.SuspendPushingNotifications = _previousState;
            }
        }
    }

    public abstract partial class AdvancedNotificationBuffer<TNotification> : AdvancedNotificationBuffer, IAdvancedNotificationBufferPresenter where TNotification : class, IAdvancedNotification {
        protected override bool HideWhenMapNotInteractive => true;
        ARAssetReference _notificationPrototypeReference;
        readonly Queue<IPAdvancedNotification<TNotification>> _notificationPresenters = new();
        
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

        void PrewarmBuffer(VisualTreeAsset prototype) {
            for (int i = 0; i < MaxVisibleNotifications; i++) {
                var presenter = MakeNotificationPresenter(prototype);
                _notificationPresenters.Enqueue(presenter);
                NotificationsParent.Add(presenter.Content);
            }
            
            IsReady = true;
            TryToPush();
        }

        protected abstract PBaseData RetrieveNotificationBaseData();
        protected abstract IPAdvancedNotification<TNotification> MakeNotificationPresenter(VisualTreeAsset prototype);

        protected override void ShowPresenterNotification(IAdvancedNotification notification) {
            if (_notificationPresenters.TryDequeue(out var freePresenter)) {
                _notificationPresenters.Enqueue(freePresenter);
                freePresenter.Show(notification as TNotification);
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            _notificationPrototypeReference?.ReleaseAsset();
            base.OnDiscard(fromDomainDrop);
        }
    }
}