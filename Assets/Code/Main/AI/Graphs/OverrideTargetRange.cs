using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.AI.Graphs {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class OverrideTargetRange : ARUnit, IGraphElementWithData {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public RangeBetween between;
        
        ARValueInput<float> _range;
        
        protected override void Definition() {
            ControlInput("start", Start);
            ControlInput("stop", Stop);
            _range = InlineARValueInput("range", 0F);
        }

        ControlOutput Start(Flow flow) {
            var ai = NpcAI(flow);
            flow.stack.GetElementData<Data>(this).cachedTargetRange = ai.TargetRange;
            ai.TargetRange = new TargetRange(between, _range.Value(flow));
            return null;
        }

        ControlOutput Stop(Flow flow) {
            NpcAI(flow).TargetRange = flow.stack.GetElementData<Data>(this).cachedTargetRange;
            return null;
        }

        class Data : IGraphElementData {
            public TargetRange cachedTargetRange;
        }

        public IGraphElementData CreateData() {
            return new Data();
        }
    }
}