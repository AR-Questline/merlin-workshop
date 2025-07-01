using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners {
    [UnityEngine.Scripting.Preserve]
    public class StaticGraphListener : GraphListener, IGraphEventListener {
        void IGraphEventListener.StartListening(GraphStack stack) => StartListening(stack);
        void IGraphEventListener.StopListening(GraphStack stack) => StopListening(stack);
        bool IGraphEventListener.IsListening(GraphPointer pointer) => IsListening(pointer);
    }
}