using Awaken.TG.VisualScripts.States;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.StateGraphs {
    [Widget(typeof(ARStateUnit))]
    public class ARStateWidget : StateWidget<ARStateUnit> {
        public ARStateWidget(StateCanvas canvas, ARStateUnit stateUnit) : base(canvas, stateUnit) { }

        protected override string summary => state.Summary ?? base.summary;
    }
}