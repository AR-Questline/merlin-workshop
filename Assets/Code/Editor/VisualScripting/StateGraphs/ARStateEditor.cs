using Awaken.TG.VisualScripts.States;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.StateGraphs {
    [Editor(typeof(ARStateUnit))]
    public class ARStateEditor : StateEditor {
        public ARStateEditor(Metadata metadata) : base(metadata) { }
    }
}