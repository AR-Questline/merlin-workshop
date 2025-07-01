using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RecurringUnit : PassiveUnit, IGraphElementWithData {

        static int s_id;

        InlineValueInput<bool> _withInstantInvoke;
        ValueInput _interval;
        ControlOutput _trigger;
        
        protected override void Definition() {
            _interval = ValueInput("interval", 1F);
            _withInstantInvoke = InlineARValueInput("withInstantInvoke", true);
            _trigger = ControlOutput("trigger");
        }

        public override void Enable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            var reference = flow.stack.AsReference();
            data.id = s_id++;
            World.Services.Get<RecurringActions>().RegisterAction(
                () => {
                    var f = AutoDisposableFlow.New(reference);
                    SafeGraph.Run(f, _trigger);
                },
                this.Skill(flow),
                GetRecurringId(data),
                flow.GetValue<float>(_interval),
                _withInstantInvoke.Value(flow)
            );
        }

        public override void Disable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            World.Services.Get<RecurringActions>().UnregisterAction(this.Skill(flow), GetRecurringId(data));
        }

        string GetRecurringId(Data data) {
            return $"RecurringUnit:{data.id}";
        }

        IGraphElementData IGraphElementWithData.CreateData() {
            return new Data();
        }
        class Data : IGraphElementData {
            public int id;
        }
    }
}