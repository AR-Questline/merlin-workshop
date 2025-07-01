using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Listeners {
    [UnityEngine.Scripting.Preserve]
    public abstract class ForEachModelsSet<T> : ARUnit, ISkillUnit where T : class, IModel {
        ValueInput _modelsSet;
        ControlInput _enter;
        ControlOutput _exit;
        ControlOutput _body;
        ValueOutput _payload;

        protected abstract string ModelName { get; }

        protected override void Definition() {
            _enter = ControlInput("enter", Loop);
            _exit = ControlOutput("exit");
            _body = ControlOutput("body");
            _payload = ValueOutput(typeof(T), ModelName);
            _modelsSet = ValueInput<ModelsSet<T>>("modelsSet");

            Succession(_enter, _body);
            Succession(_enter, _exit);
        }

        ControlOutput Loop(Flow flow) {
            var enumerator = flow.GetValue<ModelsSet<T>>(_modelsSet).GetEnumerator();
            var loop = flow.EnterLoop();
            var stack = flow.PreserveStack();

            foreach (var model in enumerator) {
                flow.SetValue(_payload, model);
                flow.Invoke(_body);
                flow.RestoreStack(stack);
                if (!flow.LoopIsNotBroken(loop)) {
                    break;
                }
            }

            flow.DisposePreservedStack(stack);
            flow.ExitLoop(loop);
            return _exit;
        }
    }

    [UnitCategory("AR/Skills"), UnitTitle("ForEach ModelsSet")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ForEachModelsSet : ForEachModelsSet<IModel> {
        protected override string ModelName => "model";
    }

    [UnitCategory("AR/Skills"), UnitTitle("ForEachStatus ModelsSet")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ForEachStatusModelsSet : ForEachModelsSet<Status> {
        protected override string ModelName => "status";
    }
}