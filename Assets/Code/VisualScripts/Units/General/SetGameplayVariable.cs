using Awaken.TG.Main.Memories;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SetGameplayVariable : ARUnit {
        protected override void Definition() {
            var name = InlineARValueInput("name", "");
            var context = FallbackARValueInput<IListenerContext>("context", _ => new AnySource());
            var value = FallbackARValueInput<object>("value", _ => default);

            var output = ControlOutput("");
            var input = ControlInput("", flow => {
                IListenerContext listenerContext = context.Value(flow);
                IModel listenerModel = listenerContext?.Model;
                GameplayMemory memory = World.Services.Get<GameplayMemory>();
                ContextualFacts facts = listenerModel != null ? memory.Context(listenerModel) : memory.Context();
                facts.Set(name.Value(flow), value.Value(flow));
                return output;
            });
            
            Succession(input, output);
        }
    }
}