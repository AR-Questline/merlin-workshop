using System;
using Awaken.TG.Main.Character;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.VisualGraphUtils {
    [UnitCategory("Generated")]
    [TypeIcon(typeof(GameObject))]
    public class GetOwnerInParent : ARGeneratedUnit {
        ControlOutput exit;
        [NullMeansSelf]
        ValueInput GameObject;
        ValueOutput CharacterView;
        ValueOutput IsCharacter;
        protected override void Definition() {
            ControlInput enter = ControlInput("Enter", Enter);
            exit = ControlOutput("Exit");
            GameObject = ValueInput<GameObject>("GameObject");
            CharacterView = ValueOutput<Component>("CharacterView");
            IsCharacter = ValueOutput<Boolean>("IsCharacter");
            Succession(enter, exit);
        }

        ControlOutput Enter(Flow flow) {
            GameObject gameObject = flow.GetValue<GameObject>(GameObject);
            ICharacterView characterView = gameObject.GetComponentInParent<ICharacterView>();
            flow.SetValue(CharacterView, characterView as Component);
            flow.SetValue(IsCharacter, characterView?.IsCharacter ?? false);
            return exit;
        }

        [UnityEngine.Scripting.Preserve]
        public static void Invoke(GameObject owner, Flow flow, Component targetComponent, out Component CharacterView, out Boolean IsCharacter) {
            Invoke(owner, flow, targetComponent.gameObject, out CharacterView, out IsCharacter);
        }
        
        public static void Invoke(GameObject owner, Flow flow, GameObject target, out Component CharacterView, out Boolean IsCharacter) {
            ICharacterView characterView = target.GetComponentInParent<ICharacterView>();
            CharacterView = characterView as Component;
            IsCharacter = characterView?.IsCharacter ?? false;
        }
    }
}