using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Custom.NotToFrequents {
    [UnitCategory("Custom/NotToFrequent")]
    [TypeIcon(typeof(Time))]
    [UnityEngine.Scripting.Preserve]
    public class NotTooFrequentData : Unit, IGraphElementWithData {
        [Serialize, Inspectable, UnitHeaderInspectable] public float[] intervals;
        
        protected override void Definition() {
            ValueOutput("", flow => flow.stack.GetElementData<Data>(this).notTooFrequent);
        }

        public IGraphElementData CreateData() => new Data(intervals);

        class Data : IGraphElementData {
            public readonly NotTooFrequent notTooFrequent;
            
            public Data(float[] intervals) {
                notTooFrequent = new NotTooFrequent(intervals);
            }
        }
    }
}