using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Links {
    [UnitCategory("Links")]
    [TypeIcon(typeof(Flow))]
    public class ValueLinkEnter : Unit, IGraphLink {
        [Serialize, Inspectable, UnitHeaderInspectable] public string label;

        public ValueInput Input { get; private set; }
        
        public string Label => label;
        
        protected override void Definition() {
            Input = ValueInput<object>("");
        }

        public override bool isControlRoot => true;
    }
}