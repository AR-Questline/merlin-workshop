using System.Collections.Generic;
using System.Linq;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Functionals {
    
    [TypeIcon(typeof(Formula))]
    public abstract class CallbackUnitBase<T> : Unit, ICustomOutputOrderUnit {
        ValueOutput _func;
        protected ControlOutput _trigger;

        public override bool isControlRoot => _func.hasValidConnection;

        protected override void Definition() {
            _func = ValueOutput("action", flow => Action(flow.stack.AsReference()));
            _trigger = ControlOutput("trigger");
        }

        protected abstract T Action(GraphReference reference);

        protected abstract IEnumerable<IUnitOutputPort> Arguments { get; }
        public IEnumerable<IUnitOutputPort> OrderedOutputs {
            get {
                yield return _func;
                yield return _trigger;
                foreach (var arg in Arguments) {
                    yield return arg;
                }
            }
        }
    }
    
    public abstract class CallbackUnit : CallbackUnitBase<Functional> {
        protected override Functional Action(GraphReference reference) {
            return new(this, reference);
        }

        protected override IEnumerable<IUnitOutputPort> Arguments => Enumerable.Empty<IUnitOutputPort>();

        public void Invoke(AutoDisposableFlow flow) {
            SafeGraph.Run(flow, _trigger);
        }
    }
    
    public abstract class CallbackUnit<T0> : CallbackUnitBase<Functional<T0>> {
        
        ValueOutput _t0;

        protected override Functional<T0> Action(GraphReference reference) {
            return new(this, reference);
        }

        protected override IEnumerable<IUnitOutputPort> Arguments {
            get {
                yield return _t0;
            }
        }

        public void Invoke(AutoDisposableFlow flow, T0 t0) {
            flow.flow.SetValue(_t0, t0);
            SafeGraph.Run(flow, _trigger);
        }
    }

    public abstract class CallbackUnit<T0, T1> : CallbackUnitBase<Functional<T0, T1>> {
        
        ValueOutput _t0;
        ValueOutput _t1;

        protected override Functional<T0, T1> Action(GraphReference reference) {
            return new(this, reference);
        }
        
        protected override IEnumerable<IUnitOutputPort> Arguments {
            get {
                yield return _t0;
                yield return _t1;
            }
        }

        public void Invoke(AutoDisposableFlow flow, T0 t0, T1 t1) {
            flow.flow.SetValue(_t0, t0);
            flow.flow.SetValue(_t1, t1);
            SafeGraph.Run(flow, _trigger);
        }
    }
    
    public abstract class CallbackUnit<T0, T1, T2> : CallbackUnitBase<Functional<T0, T1, T2>> {
        
        ValueOutput _t0;
        ValueOutput _t1;
        ValueOutput _t2;

        protected override Functional<T0, T1, T2> Action(GraphReference reference) {
            return new(this, reference);
        }
        
        protected override IEnumerable<IUnitOutputPort> Arguments {
            get {
                yield return _t0;
                yield return _t1;
                yield return _t2;
            }
        }

        public void Invoke(AutoDisposableFlow flow, T0 t0, T1 t1, T2 t2) {
            flow.flow.SetValue(_t0, t0);
            flow.flow.SetValue(_t1, t1);
            flow.flow.SetValue(_t2, t2);
            SafeGraph.Run(flow, _trigger);
        }
    }
}