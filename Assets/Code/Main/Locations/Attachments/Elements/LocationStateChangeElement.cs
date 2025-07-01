using System;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class LocationStateChangeElement : Element<Location>, IRefreshedByAttachment<LocationStateChangeAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationStateChangeElement;

        [Saved] ARDateTime _changeStateTime;
        LocationStateChangeAttachment _spec;
        TimedEvent _automaticChangeTimedEvent;
        
        public void InitFromAttachment(LocationStateChangeAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            if (_spec.OnInteract) {
                ParentModel.ListenTo(Location.Events.Interacted, ManualTriggerChangeState, this);
            }

            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }

        protected void OnVisualLoaded(Transform t) {
            if (_spec.AfterTime || _spec.AtSpecificTime) {
                if (_changeStateTime != default) {
                    CreateTimedEvent(_changeStateTime);
                } else {
                    ChangeStateAfterTime();
                }
            }
        }

        void ChangeStateAfterTime() {
            ARDateTime afterTimeChange = default;
            ARDateTime atSpecificTimeChange = default;
            var currentTime = World.Any<GameRealTime>().WeatherTime;

            if (_spec.AfterTime) {
                afterTimeChange = currentTime + _spec.MinimumTime;
                if (!_spec.AtSpecificTime) {
                    CreateTimedEvent(afterTimeChange);
                    return;
                }
            }

            if (_spec.AtSpecificTime) {
                atSpecificTimeChange = new ARDateTime(new DateTime(currentTime.Year, currentTime.Month,
                    currentTime.DayOfTheMonth, _spec.SpecificTime.Hours, _spec.SpecificTime.Minutes, 0));
                if (atSpecificTimeChange <= currentTime) {
                    atSpecificTimeChange += new TimeSpan(1, 0, 0, 0);
                }

                if (!_spec.AfterTime) {
                    CreateTimedEvent(atSpecificTimeChange);
                    return;
                }
            }

            if (afterTimeChange < atSpecificTimeChange) {
                CreateTimedEvent(_spec.UseLongerTime ? atSpecificTimeChange : afterTimeChange);
            } else {
                CreateTimedEvent(_spec.UseLongerTime ? afterTimeChange : atSpecificTimeChange);
            }
        }

        void CreateTimedEvent(ARDateTime changeStateTime) {
            _changeStateTime = changeStateTime;
            _automaticChangeTimedEvent = new TimedEvent(changeStateTime.Date, ChangeState);
            World.Any<GameTimeEvents>().AddEvent(_automaticChangeTimedEvent);
        }

        // Manual change should change state after 1 frame to avoid race conditions on attachment group changes
        void ManualTriggerChangeState() {
            TriggerChangeStateAfterFrame().Forget();
        }

        async UniTaskVoid TriggerChangeStateAfterFrame() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            } 
            ChangeState();
        }

        void ChangeState() {
            ParentModel?.TryGetElement<LocationStatesElement>()?.ChangeState(_spec.NewState);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_automaticChangeTimedEvent != null) {
                World.Any<GameTimeEvents>()?.RemoveEvent(_automaticChangeTimedEvent);
                _automaticChangeTimedEvent = null;
            }
        }
    }
}