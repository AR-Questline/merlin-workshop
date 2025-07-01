using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Events {
    public abstract class ListenerUnit : ARUnit, IEventUnit, IGraphElementWithData {
        public override bool isControlRoot => true;
        
        public abstract bool coroutine { get; }
        protected abstract IEventListener Listener(Flow flow);

        void IGraphEventListener.StartListening(GraphStack stack) {
            var data = stack.GetElementData<ListenerData>(this);
            using var flow = Flow.New(stack.AsReference());
            data.listener = Listener(flow);
        }

        void IGraphEventListener.StopListening(GraphStack stack) {
            var data = stack.GetElementData<ListenerData>(this);
            if (data.listener != null) {
                World.EventSystem.RemoveListener(data.listener);
                data.listener = null;
            }
        }
        
        bool IGraphEventListener.IsListening(GraphPointer pointer) {
            return pointer.GetElementData<ListenerData>(this).listener != null;
        }

        [UnityEngine.Scripting.Preserve]
        protected static AutoDisposableFlow CreateSelfDisposableFlow(GraphReference reference) {
            return AutoDisposableFlow.New(reference);
        }
        
        protected class ListenerData : IGraphElementData {
            public IEventListener listener;
        }

        IGraphElementData IGraphElementWithData.CreateData() {
            return new ListenerData();
        }
    }
}