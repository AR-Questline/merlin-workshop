using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Timing {
    public class RecurringActions : IService {

        BinaryHeap<RecurringAction> _recurringHeap = new(new RecurringActionComparer(), 100);

        public void Init() {
            World.Add(new TimeModel(Domain.Globals)).GetOrCreateTimeDependent().WithUpdate(OnUpdate).ThatProcessWhenPause();
        }

        void OnUpdate(float deltaTime) {
            while (!_recurringHeap.IsEmpty && (_recurringHeap.Peek.data == null || _recurringHeap.Peek.data.nextCallTime <= Time.time)) {
                var recurring = _recurringHeap.Extract();
                if (recurring.data != null) {
                    recurring.data.nextCallTime += recurring.data.interval;
                    _recurringHeap.Insert(recurring);
                    recurring.data.action();
                }
            }
        }

        public void RegisterAction(Action action, IModel model, string id, float interval, bool withInstantInvoke = true) {
            RegisterAction(action, ActionID(model, id), interval, withInstantInvoke);
        }
        public void RegisterAction(Action action, string id, float interval, bool withInstantInvoke = true) {
            if (interval <= 0) throw new ArgumentException("Interval must by greater than zero");
            _recurringHeap.Insert(new RecurringAction(action, id, interval, withInstantInvoke));
        }
        
        [UnityEngine.Scripting.Preserve] 
        public void RegisterDelayedAction(Action action, float delay) {
            _recurringHeap.Insert(RecurringAction.DelayedAction(action, delay));
        }
        
        public void UnregisterAction(IModel model, string id) {
            UnregisterAction(ActionID(model, id));
        }
        public void UnregisterAction(string id) {
            foreach (var recurring in _recurringHeap.Where(r => r.data != null && r.data.id == id)) {
                recurring.data = null;
            }
        }

        public void UpdateActionInterval(string id, float newInterval, bool withInstantInvoke = true) {
            var toChange = _recurringHeap.Where(r => r.data != null && r.data.id == id);
            foreach (RecurringAction recurringAction in toChange.ToList()) {
                _recurringHeap.Insert(new RecurringAction(recurringAction.data, newInterval, withInstantInvoke));
                recurringAction.data = null;
            }
        }
        
        [UnityEngine.Scripting.Preserve] 
        public void UpdateActionInterval(string id, IModel model, float newInterval, bool withInstantInvoke = true) {
            string actionID = ActionID(model, id);
            var toChange = _recurringHeap.Where(r => r.data != null && r.data.id == actionID);
            foreach (RecurringAction recurringAction in toChange.ToList()) {
                _recurringHeap.Insert(new RecurringAction(recurringAction.data, newInterval, withInstantInvoke));
                recurringAction.data = null;
            }
        }
        

        static string ActionID(IModel model, string id) {
            return $"{model.ID}:{id}";
        }

        public class RecurringData {
            public string id;
            public float nextCallTime;
            public float interval;
            public Action action;
        }
        
        public class RecurringAction {
            public RecurringData data;

            RecurringAction() { }

            public RecurringAction(Action action, string id, float interval, bool withInstantInvoke) {
                data = new RecurringData() {
                    action = action,
                    id = id,
                    interval = interval,
                    nextCallTime = Time.time + (withInstantInvoke ? 0 : interval)
                };
            }
            public RecurringAction(RecurringData dataToUse, float newInterval, bool withInstantInvoke) {
                data = new RecurringData {
                    action = dataToUse.action,
                    id = dataToUse.id,
                    interval = newInterval,
                    nextCallTime = Time.time + (withInstantInvoke ? 0 : newInterval)
                };
            }

            public static RecurringAction DelayedAction(Action action, float delay) {
                var delayed = new RecurringAction {data = new RecurringData()};
                delayed.data.action = () => {
                    action();
                    delayed.data = null;
                };
                delayed.data.nextCallTime = Time.time + delay;
                return delayed;
            }
        }
        
        class RecurringActionComparer : IComparer<RecurringAction> {
            public int Compare(RecurringAction x, RecurringAction y) {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(y?.data, null)) return 1;
                if (ReferenceEquals(x?.data, null)) return -1;
                return x.data.nextCallTime.CompareTo(y.data.nextCallTime);
            }
        }
        
    }
    
}