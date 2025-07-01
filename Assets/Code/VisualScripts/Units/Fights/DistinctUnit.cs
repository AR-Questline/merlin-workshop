using System.Collections.Generic;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    public abstract class DistinctUnit<TObj, TKey> : Unit, IGraphElementWithData {
        ControlOutput _started;
        ControlOutput _stopped;
        
        ControlInput _try;
        ValueInput _object;

        ControlOutput _distinct;

        class Data : IGraphElementData {
            public bool enabled;
            public readonly HashSet<TKey> key = new();
        }

        protected override void Definition() {
            _started = ControlOutput("started");
            _stopped = ControlOutput("stopped");
            
            ControlInput("start", Start);
            ControlInput("stop", Stop);

            _try = ControlInput("try", Enter);
            _object = ValueInput<TObj>("object");

            _distinct = ControlOutput("distinct");

            Succession(_try, _distinct);
        }

        ControlOutput Start(Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            data.enabled = true;
            data.key.Clear();
            return _started;
        }

        ControlOutput Stop(Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            data.enabled = false;
            data.key.Clear();
            return _stopped;
        }

        ControlOutput Enter(Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);

            if (!data.enabled) return null;

            var obj = flow.GetValue<TObj>(_object);
            var key = GetKey(obj);
            
            if (key != null && data.key.Add(key)) {
                SetOutput(flow, obj, key);
                return _distinct;
            } else {
                return null;
            }
        }
        
        protected abstract TKey GetKey(TObj obj);
        protected abstract void SetOutput(Flow flow, TObj obj, TKey key);

        IGraphElementData IGraphElementWithData.CreateData() {
            return new Data();
        }
    }
}