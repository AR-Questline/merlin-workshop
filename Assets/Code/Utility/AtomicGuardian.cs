namespace Awaken.Utility {
    /// <summary>
    /// It ensures that nested operation calls will not interrupt each other, but execute sequentially. <br/>
    /// Each call should be guarded by a bool which tells if it was requested
    /// </summary>
    public abstract class AtomicGuardian<TOuter> where TOuter : class {
        readonly TOuter _outer;
        bool _changing;
        bool _wasRequestCalled;

        protected AtomicGuardian(TOuter outer) {
            _outer = outer;
        }

        protected void Call(ref bool id) {
            id = true;
            if (!_changing) {
                CheckRequests();
            }
        }

        protected bool Requested(ref bool id) {
            _wasRequestCalled = id;
            id = false;
            return _wasRequestCalled;
        }

        /// <summary> Triggers all requested calls </summary>
        void CheckRequests() {
            _changing = true;
            do {
                _wasRequestCalled = false;
                CheckRequest(_outer);
            } while (_wasRequestCalled);
            _changing = false;
        }

        /// <summary>
        /// Trigger one of requested calls. <br/>
        /// To check if call was requested it should use method Requested
        /// </summary>
        /// <example><code>
        /// if (Requested(ref _op1)) {
        ///     outer.Op1();
        /// } else if (Requested(ref _op2)) {
        ///     outer.Op2();
        /// } else if (Requested(ref _op3)) {
        ///     outer.Op3();
        /// }
        /// </code></example>
        protected abstract void CheckRequest(TOuter outer);
    }
}