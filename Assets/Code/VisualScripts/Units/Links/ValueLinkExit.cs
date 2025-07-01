using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Links {
    [UnitCategory("Links")]
    [TypeIcon(typeof(Flow))]
    public class ValueLinkExit : Unit, IGraphLink {
        [Serialize, Inspectable, UnitHeaderInspectable] public string label;

        bool _hasCachedInput;
        ValueInput _cachedInput;
        
        public string Label => label;
        
        protected override void Definition() {
            ValueOutput("", flow => flow.GetValue(GetLinkedInput()));
        }
        
        ValueInput GetLinkedInput() {
            if (!_hasCachedInput) {
                _cachedInput = RetrieveLinkedInput();
                _hasCachedInput = true;
            }
            return _cachedInput;
        }

        ValueInput RetrieveLinkedInput() {
            foreach (var unit in graph.units) {
                if (unit is ValueLinkEnter enter) {
                    if (enter.label == label) {
                        return enter.Input;
                    }
                }
            }
            return null;
        }
    }
}