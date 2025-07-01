using System.Collections;
using Awaken.TG.Main.Heroes.Resting;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.VisualGraphUtils {
    [UnitCategory("AR/Time")]
    [UnityEngine.Scripting.Preserve]
    public class SpawnRestPopupUI : ARUnit {
        ControlInput _enter;
        ControlOutput _onDiscard;
        InlineValueInput<bool> _withTransition;
        
        protected override void Definition() {
            _withTransition = InlineARValueInput("with fade transition", true);
            _enter = ControlInputCoroutine("enter", Await);
            _onDiscard = ControlOutput("onDiscard");
            Succession(_enter, _onDiscard);
        }
        
        IEnumerator Await(Flow flow) {
            var restPopupUI = new RestPopupUI(null, _withTransition.Value(flow));
            World.Add(restPopupUI);
            while (!restPopupUI.WasDiscarded) {
                yield return null;
            }
            yield return _onDiscard;
        }
    }
}