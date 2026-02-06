using Awaken.TG.VisualScripts.Units;
using Awaken.Utility.GameObjects;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnitCategory("AR/Skills/Getters")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetChildByTag : ARUnit, ISkillUnit {
        protected override void Definition() {
            var tagToFind = RequiredARValueInput<string>("tag");
            var gameobject = RequiredARValueInput<UnityEngine.GameObject>("gameObject");
            ValueOutput("game object", flow => gameobject.Value(flow).FindChildWithTagRecursively(tagToFind.Value(flow)).gameObject);
        }
    }
}