using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Utils {
    /// <summary>
    /// Flow needs to be disposed, but it's not straightforward if called method will do it or not.
    /// So we wrap it in this struct in situations where flow will dispose itself.
    /// </summary>
    public struct AutoDisposableFlow {
        public Flow flow;

        public AutoDisposableFlow(Flow flow) {
            this.flow = flow;
        }

        public static AutoDisposableFlow New(GraphReference reference) {
            return new(Flow.New(reference));
        }
    }
}
