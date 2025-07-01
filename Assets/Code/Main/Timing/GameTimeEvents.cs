using System;
using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Timing {
    public partial class GameTimeEvents : Element<GameRealTime> {
        public sealed override bool IsNotSaved => true;

        BinaryHeap<TimedEvent> _timeHeap = new(new TimedEventComparer());

        protected override void OnInitialize() {
            ParentModel.ListenTo(GameRealTime.Events.GameTimeChanged, OnGameTimeChanged, this);
        }

        void OnGameTimeChanged(ARDateTime newTime) {
            while (!_timeHeap.IsEmpty && _timeHeap.Peek.Time <= newTime.Date) {
                TimedEvent timedEvent = _timeHeap.Extract();
                timedEvent.Action();
            }
        }

        public void AddEvent(TimedEvent timedEvent) {
            _timeHeap.Insert(timedEvent);
        }

        public void RemoveEvent(TimedEvent timedEvent) {
            _timeHeap.Remove(timedEvent);
        }
    }

    public class TimedEvent {
        public DateTime Time { get; }
        public Action Action { get; }

        public TimedEvent(DateTime time, Action action) {
            Time = time;
            Action = action;
        }
    }
    
    class TimedEventComparer : IComparer<TimedEvent> {
        public int Compare(TimedEvent x, TimedEvent y) {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return x.Time.CompareTo(y.Time);
        }
    }
}