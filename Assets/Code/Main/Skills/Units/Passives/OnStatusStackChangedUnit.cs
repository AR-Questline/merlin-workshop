using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class OnStatusStackChangedUnit : PassiveListenerWithPayloadUnit<Status, int> {
        ARValueInput<Status> _status;

        protected override void Definition() {
            base.Definition();
            _status = RequiredARValueInput<Status>("status");
        }

        protected override string DataName => "Stack Level";
        protected override Status Source(Skill skill, Flow flow) => _status.Value(flow);
        protected override Event<Status, int> Event(Skill skill, Flow flow) => Status.Events.StatusStackChanged;
    }
}