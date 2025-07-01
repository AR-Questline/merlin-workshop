using System;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.VisualScripts.Units.Utils {
    public static class SafeGraph {

        public static void Trigger(string name, GameObject go) {
            try {
                TriggerRethrow(name, go);
            } catch (Exception e) {
                Debug.LogException(e, go);
            }
        }
        public static void TriggerRethrow(string name, GameObject go) {
            try {
                EventBus.Trigger(name, go);
            } catch {
                Log.Important?.Error($"[SafeGraph] Exception for event: {name} for GameObject: {go.PathInSceneHierarchy()}", go);
                throw;
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void Trigger<TArgs>(string name, GameObject go, TArgs args) {            
            try {
                TriggerRethrow(name, go, args);
            } catch (Exception e) {
                Debug.LogException(e, go);
            }
        }
        public static void TriggerRethrow<TArgs>(string name, GameObject go, TArgs args) {
            try {
                EventBus.Trigger(name, go, args);
            } catch {
                Log.Important?.Error($"[SafeGraph] Exception for event: {name} for GameObject: {go.PathInSceneHierarchy()}", go);
                throw;
            }
        }
        
        public static void Run(AutoDisposableFlow flow, ControlOutput port) {
            try {
                RunRethrow(flow, port);
            } catch (Exception e) {
                Debug.LogException(e, flow.flow.stack.self);
            }
        }

        public static void RunRethrow(AutoDisposableFlow flow, ControlOutput port) {
            try {
                flow.flow.Run(port);
            } catch {
                var obj = flow.flow.stack.AsReference().serializedObject;
                Log.Important?.Error($"[SafeGraph] Exception for graph {obj.name} from trigger:\n{new Data(port)}", obj);
                throw;
            }
        }

        public static T GetValue<T>(Flow flow, ValueInput port) {
            try {
                return GetValueReThrow<T>(flow, port);
            } catch (Exception e) {
                Debug.LogException(e, flow.stack.self);
                return default;
            }
        }
        public static T GetValueReThrow<T>(Flow flow, ValueInput port) {
            try {
                return flow.GetValue<T>(port);
            } catch {
                var obj = flow.stack.AsReference().serializedObject;
                Log.Important?.Error($"[SafeGraph] Exception for graph {obj.name} from trigger:\n{new Data(port)}", obj);
                throw;
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public static T GetValue<T>(GraphReference reference, ValueInput port) {
            using var flow = Flow.New(reference);
            return GetValue<T>(flow, port);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static T GetValueReThrow<T>(GraphReference reference, ValueInput port) {
            using var flow = Flow.New(reference);
            return GetValueReThrow<T>(flow, port);
        }

        public struct Data {
            public Guid guid;
            public string additional;

            public Data(IUnit unit) {
                guid = unit.guid;
                additional = null;
            }
            
            public Data(IUnitPort port) : this(port.unit) {
                additional = port.key;
            }
            
            public override string ToString() {
                return $"{guid}\n" +
                       $"{additional}";
            }

            public static Data Parse(string text) {
                var lines = text.Split('\n');
                return new Data {
                    guid = Guid.Parse(lines[0]),
                    additional = lines[1],
                };
            }
        }
    }
}