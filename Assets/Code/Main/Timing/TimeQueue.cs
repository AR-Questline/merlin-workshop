using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using Unity.Profiling;
using UnityEngine;
using UniversalProfiling;
using PQNode = Awaken.Utility.Collections.PriorityQueue<string, Awaken.TG.Main.Timing.TimeAction>.PQNode;

namespace Awaken.TG.Main.Timing {
    public class TimeQueue : IDomainBoundService {
        static readonly UniversalProfilerMarker UpdateMarker = new("TimeQueue: PopInvoke");
        static readonly UniversalProfilerMarker ReAddMarker = new("TimeQueue: Update");
        
        public Domain Domain => Domain.CurrentMainSceneOrPreviousMainSceneWhileDropping();
        public bool RemoveOnDomainChange() {
            _queue.Clear();
            return false;
        }

        readonly PriorityQueue<string, TimeAction> _queue = new();
        readonly List<PQNode> _cache = new(16);
        
        public void Init() {
            World.Add(new TimeModel(Domain.Globals)).GetOrCreateTimeDependent().WithUpdate(Update).ThatProcessWhenPause();
        }

        void Update(float deltaTime) {
            ReAddMarker.Begin();
            var currentTime = Time.time;
            _cache.Clear();
            
            foreach (PQNode node in _queue.AllNodesBelow(currentTime)) {
                _cache.Add(node);
                _queue.UnlinkNode(node);
                
                UpdateMarker.Begin();
                try {
                    node.item.action.Invoke();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
                UpdateMarker.End();
                
                node.priority += node.item.interval;
            }
            
            _queue.RePrioritizeRange(_cache);
            ReAddMarker.End();
        }

        // Possible improvement. deffer adding to end of frame and then batch add
        public void Register(TimeAction action) { 
            if (action.action == null) return;
            _queue.Add(action.id, Time.time + action.interval, action);
        }
        [UnityEngine.Scripting.Preserve] 
        public void RegisterRange(IEnumerable<TimeAction> action) {
            var currentTime = Time.time;
            _queue.AddRange(action.Where(a => a.action != null).Select(a => new PQNode(a.id, currentTime + a.interval, a)));
        }

        public void Unregister(string id) {
            for (int i = _cache.Count - 1; i >= 0; i--) {
                if (_cache[i].id == id) {
                    _queue.RemoveNode(_cache[i]);
                    _cache.RemoveAt(i);
                    return;
                }
            }
            _queue.Remove(id);
        }

        // Possible improvement. deffer adding to end of frame and then batch rePrioritize
        [UnityEngine.Scripting.Preserve] 
        public void UpdateInterval(string id, float newInterval) {
            LinkedListNode<PQNode> internalNode = _queue.ReadInternalNode(id);
            internalNode.Value.item.interval = newInterval;
            _queue.RePrioritize(internalNode, Time.time + newInterval);
        }
    }

    public class TimeAction {
        public readonly string id;
        public float interval;
        public Action action;
        
        [UnityEngine.Scripting.Preserve] 
        public TimeAction(string id, Action action, float interval) {
            this.id = id;
            this.action = action;
            this.interval = interval;
        }
        public TimeAction(IModel model, Action action, float interval) {
            this.id = model.ContextID;
            this.action = action;
            this.interval = interval;
        }
    }

}