using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ModifyCharacterProjectileDeflection : ARUnit {
        RequiredValueInput<ICharacter> _target;
        OptionalValueInput<bool> _setEnabledTo;
        OptionalValueInput<bool> _setTargetEnemies;
        OptionalValueInput<DamageType> _enableForType;
        OptionalValueInput<DamageType> _disableForType;
        OptionalValueInput<DamageType[]> _setTypes;

        protected override void Definition() {
            _target = RequiredARValueInput<ICharacter>("Target");
            _setEnabledTo = OptionalARValueInput<bool>("Set Enabled To");
            _setTargetEnemies = OptionalARValueInput<bool>("Set Target Enemies");
            _enableForType = OptionalARValueInput<DamageType>("Enable For Type");
            _disableForType = OptionalARValueInput<DamageType>("Disable For Type");
            _setTypes = OptionalARValueInput<DamageType[]>("Override Types");
            
            ControlInput("start", Invoke);
            ControlOutput("end");
        }

        ControlOutput Invoke(Flow flow) {
            CharacterProjectileDeflection target = CharacterProjectileDeflection.GetOrCreate(_target.Value(flow));
            
            if (target == null) {
                return null;
            }
            
            if (_setEnabledTo.HasValue) {
                if (_setEnabledTo.Value(flow)) {
                    target.EnableDeflection();
                } else {
                    target.DisableDeflection();
                }
            }
            
            if (_setTargetEnemies.HasValue) {
                if (_setTargetEnemies.Value(flow)) {
                    target.SetDeflectionTargetEnemy();
                } else {
                    target.SetDeflectionDirectionRandom();
                }
            }
            
            if (_enableForType.HasValue) {
                target.EnableDeflectionForType(_enableForType.Value(flow));
            }
            
            if (_disableForType.HasValue) {
                target.DisableDeflectionForType(_disableForType.Value(flow));
            }
            
            if (_setTypes.HasValue) {
                target.SetDeflectionTypes(_setTypes.Value(flow));
            }
            
            return null;
        }
    }
}