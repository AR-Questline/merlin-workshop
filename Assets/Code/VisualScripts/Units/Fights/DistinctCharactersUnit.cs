using Awaken.TG.Main.Character;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class DistinctCharactersUnit : DistinctUnit<GameObject, ICharacter> {
        
        ValueOutput _character;
        ValueOutput _characterView;

        protected override void Definition() {
            base.Definition();
            
            _character = ValueOutput<ICharacter>("character");
            _characterView = ValueOutput<GameObject>("characterView");
        }

        protected override ICharacter GetKey(GameObject obj) {
            return VGUtils.GetModel<ICharacter>(obj);
        }

        protected override void SetOutput(Flow flow, GameObject obj, ICharacter key) {
            flow.SetValue(_character, key);
            flow.SetValue(_characterView, key.CharacterView.transform.gameObject);
        }
    }
}