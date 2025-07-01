using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Functionals {
    [TypeIcon(typeof(Formula))]
    public class Functional {
        CallbackUnit _callback;
        GraphReference _reference;

        public Functional(CallbackUnit callback, GraphReference reference) {
            _callback = callback;
            _reference = reference;
        }

        [UnityEngine.Scripting.Preserve]
        public void Invoke() {
            _callback.Invoke(AutoDisposableFlow.New(_reference));
        }
    }

    [TypeIcon(typeof(Formula))]
    public class Functional<T0> {
        CallbackUnit<T0> _callback;
        GraphReference _reference;

        public Functional(CallbackUnit<T0> callback, GraphReference reference) {
            _callback = callback;
            _reference = reference;
        }

        [UnityEngine.Scripting.Preserve]
        public void Invoke(T0 t0) {
            _callback.Invoke(AutoDisposableFlow.New(_reference), t0);
        }
    }
    
    [TypeIcon(typeof(Formula))]
    public class Functional<T0, T1> {
        CallbackUnit<T0, T1> _callback;
        GraphReference _reference;

        public Functional(CallbackUnit<T0, T1> callback, GraphReference reference) {
            _callback = callback;
            _reference = reference;
        }

        [UnityEngine.Scripting.Preserve]
        public void Invoke(T0 t0, T1 t1) {
            _callback.Invoke(AutoDisposableFlow.New(_reference), t0, t1);
        }
    }
    
    [TypeIcon(typeof(Formula))]
    public class Functional<T0, T1, T2> {
        CallbackUnit<T0, T1, T2> _callback;
        GraphReference _reference;

        public Functional(CallbackUnit<T0, T1, T2> callback, GraphReference reference) {
            _callback = callback;
            _reference = reference;
        }

        [UnityEngine.Scripting.Preserve]
        public void Invoke(T0 t0, T1 t1, T2 t2) {
            _callback.Invoke(AutoDisposableFlow.New(_reference), t0, t1, t2);
        }
    }
}