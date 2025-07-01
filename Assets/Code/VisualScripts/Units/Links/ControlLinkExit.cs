using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Links {
    [UnitCategory("Links")]
    [TypeIcon(typeof(Flow))]
    public class ControlLinkExit : Unit, IGraphLink {
        [Serialize, Inspectable, UnitHeaderInspectable] public string label;
        
        public ControlOutput Output { get; private set; }
        
        public string Label => label;
        
        protected override void Definition() {
            Output = ControlOutput("");
        }
        
        public override bool isControlRoot => true;
    }
}