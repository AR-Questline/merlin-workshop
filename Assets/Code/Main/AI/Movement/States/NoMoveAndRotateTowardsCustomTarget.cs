using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.Character;

namespace Awaken.TG.Main.AI.Movement.States {
    public class NoMoveAndRotateTowardsCustomTarget : NoMoveAndRotateTowardsTarget {
        readonly ICharacter _target;
        
        protected override IRotationScheme RotationScheme => new RotateTowardsCustomTarget(_target);

        public NoMoveAndRotateTowardsCustomTarget(ICharacter target) {
            _target = target;
        }
    }
}