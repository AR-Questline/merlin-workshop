using Awaken.TG.Main.Animations.FSM.Heroes.States.Block;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Utility;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class PushAwayFromOwner : ARUnit, ISkillUnit {
        protected override void Definition() {
            var character = RequiredARValueInput<ICharacter>("characterToPush");
            var ragdollForce = FallbackARValueInput("ragdollForce", _ => 25f);
            var ownerInput = FallbackARValueInput("owner", f => this.Skill(f).Owner);
            DefineSimpleAction("Enter", "Exit", flow => {
                ICharacter toPush = character.Value(flow);
                ICharacter owner = ownerInput.Value(flow);
                Vector3 direction = (toPush.Coords - owner.Coords).ToHorizontal3();
                DamageParameters parameters = DamageParameters.Default;
                parameters.Direction = direction;
                parameters.ForceDirection = direction;
                parameters.ForceDamage = 0;
                parameters.RagdollForce = ragdollForce.Value(flow);
                parameters.PoiseDamage = 0;
                parameters.IsPush = true;
                parameters.Inevitable = true;
                Damage damage = new(parameters, owner, toPush, new RawDamageData(0));
                damage.WithStaminaDamage(BlockPommel.PommelStaminaDamage);
                toPush.HealthElement.TakeDamage(damage);
            });
        }
    }
}