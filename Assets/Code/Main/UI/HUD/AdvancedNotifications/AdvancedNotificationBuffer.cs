using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.BufferBlockers;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    public abstract partial class AdvancedNotificationBuffer : Element<HUD> {
        public static bool AllNotificationsSuspended { get; set; }
        public sealed override bool IsNotSaved => true;
        
        public bool IsPushing { get; protected set; }
        
        public abstract void ChangeForceVisible(bool value);
        
        public new static class Events {
            public static readonly Event<AdvancedNotificationBuffer, bool> BeforePushingFirstNotification = new(nameof(BeforePushingFirstNotification));
            public static readonly Event<AdvancedNotificationBuffer, bool> AfterPushingLastNotification = new(nameof(AfterPushingLastNotification));
            public static readonly Event<AdvancedNotificationBuffer, AdvancedNotification> AfterPushingNewNotification = new(nameof(AfterPushingNewNotification));
        }
    }
    
    public abstract partial class AdvancedNotificationBuffer<TNotification> : AdvancedNotificationBuffer where TNotification : AdvancedNotification {
        protected readonly Queue<TNotification> notificationQueue = new();
        
        int _shownCounter;
        UIStateStack _stateStack;
        bool _forceVisible;
        bool _suspendPushingNotifications;
        HudBackgroundsIntensity _hudBackgroundsIntensity;
        IEventListener _bufferBlockerListener;
        IEventListener _dependentBufferListener;
        
        protected IEventListener _stateStackListener;

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

        protected override void OnInitialize() {
            _stateStack = UIStateStack.Instance;
            _stateStackListener = _stateStack.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
        }

        protected override void OnFullyInitialized() {
            IsReady = true;
            TryToPush();
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
                if (notificationQueue.TryDequeue(out TNotification notification)) {
                    if (notification.HasBeenDiscarded) {
                        continue;
                    }

                    if (!notification.IsValid) {
                        notification.Discard();
                        continue;
                    }

                    if (notification.IsMergeable) {
                        MergeSimilarNotifications(notification);
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
        
        protected virtual void MergeSimilarNotifications(TNotification notification) { }
        
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
            int minToPush = Mathf.Min(notificationQueue.Count, MaxVisibleNotifications);
            if (!ShouldBeHidden && minToPush > 0) {
                for (var i = 0; i < minToPush; i++) {
                    TryToPush();
                }
            }

            if (!ShouldBeHidden && _shownCounter == 0 && notificationQueue.Count == 0 && IsPushing) {
                SetBufferCanvasGroupAlpha(0f);
                TryToPush();
            }
        }

        void OnVisibleDiscard(IModel discarded) {
            _shownCounter--;
            TryToPush();
        }

        void ShowNotification(TNotification notification) {
            notification.Show(); //old way of showing notifications - to be removed when all notifications are converted to UIToolkit
            ShowPresenterNotification(notification); //new way of showing notifications with UIToolkit
            _shownCounter++;
            notification.ListenTo(Model.Events.AfterDiscarded, OnVisibleDiscard, this);
            this.Trigger(Events.AfterPushingNewNotification, notification);
            OnAfterPushingNewNotification();

            if (notificationQueue.Count == 0) {
                OnBeforePushingLastNotification();
            }
        }

        void SetBufferCanvasGroupAlpha(float alpha) {
            if (BufferCanvasGroup != null) {
                BufferCanvasGroup.alpha = alpha;
            }

            NotificationsParent?.SetActiveOptimized(alpha > 0f);
        }

        public void PushNotification(TNotification notificationElement) {
            if (SuspendPushingNotifications || notificationElement == null) {
                return;
            }
            
            var notifications = Elements<AdvancedNotification>();
            if (notifications.Count() + 1 > MaxVisibleNotifications && StrictMaxVisibleNotifications) {
                return;
            }
            
            AddElement(notificationElement);
            notificationQueue.Enqueue(notificationElement);
            TryToPush();
        }

        public void ClearBuffer() {
            var notifications = Elements<AdvancedNotification>().ToArraySlow();
            foreach (var notification in notifications) {
                notification.Discard();
            }
            
            notificationQueue.Clear();
        }

        public override void ChangeForceVisible(bool value) {
            if (_forceVisible == value) {
                return;
            }
            _forceVisible = value;
            OnUIStateChanged(_stateStack.State);
        }

        protected virtual void ShowPresenterNotification(TNotification notification) { }

        public class SuspendNotifications<T> : IDisposable where T : AdvancedNotificationBuffer<TNotification> {
            readonly AdvancedNotificationBuffer<TNotification> _buffer;
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
}