using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners {
    [UnityEngine.Scripting.Preserve]
    public class SpecificGraphListener : GraphListener {

        ControlOutput _onStart;
        ControlOutput _onStop;
        
        protected override void Definition() {
            ControlInput("start", Start);
            ControlInput("stop", Stop);
            _onStart = ControlOutput("onStart");
            _onStop = ControlOutput("onStop");
            base.Definition();
        }

        ControlOutput Start(Flow flow) {
            StartListening(flow.stack);
            return _onStart;
        }
        
        ControlOutput Stop(Flow flow) {
            StopListening(flow.stack);
            return _onStop;
        }
    }
}