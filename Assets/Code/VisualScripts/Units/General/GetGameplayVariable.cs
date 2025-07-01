using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetGameplayVariable : ARUnit {
        protected override void Definition() {
            var name = InlineARValueInput("name", "");
            var context = FallbackARValueInput<IListenerContext>("context", _ => new AnySource());
            var fallback = FallbackARValueInput<object>("fallback", _ => default);

            ValueOutput("", flow => {
                IListenerContext listenerContext = context.Value(flow);
                IModel listenerModel = listenerContext?.Model;
                GameplayMemory memory = World.Services.Get<GameplayMemory>();
                ContextualFacts facts = listenerModel != null ? memory.Context(listenerModel) : memory.Context();
                return facts.Get(name.Value(flow), fallback.Value(flow));
            });
        }
    }
}