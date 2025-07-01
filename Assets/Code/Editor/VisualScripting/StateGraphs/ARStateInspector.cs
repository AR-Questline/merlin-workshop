using Awaken.TG.VisualScripts.States;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.StateGraphs {
    [Inspector(typeof(ARStateUnit))]
    public class ARStateInspector : ReflectedInspector {
        public ARStateInspector(Metadata metadata) : base(metadata) { }
    }
}