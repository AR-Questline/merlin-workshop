using Awaken.TG.Main.Character;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class DistinctAliveUnit : DistinctUnit<GameObject, IAlive> {

        ValueOutput _alive;

        protected override void Definition() {
            base.Definition();

            _alive = ValueOutput<IAlive>("alive");
        }

        protected override IAlive GetKey(GameObject obj) {
            return VGUtils.GetModel<IAlive>(obj);
        }

        protected override void SetOutput(Flow flow, GameObject obj, IAlive key) {
            flow.SetValue(_alive, key);
        }
    }
}