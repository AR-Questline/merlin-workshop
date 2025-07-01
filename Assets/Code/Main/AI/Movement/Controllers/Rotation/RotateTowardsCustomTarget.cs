using Awaken.TG.Main.Character;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.AI.Movement.Controllers.Rotation {
    public class RotateTowardsCustomTarget : IRotationScheme {
        public NpcController Controller { get; set; }
        public bool UseRichAIRotation => _target == null;
        readonly IGrounded _target;

        public RotateTowardsCustomTarget(IGrounded target) {
            _target = target;
        }
        
        public void Enter() { }
        
        public void Update(float deltaTime) {
            if (_target is {HasBeenDiscarded: false}) {
                Controller.SteeringDirection = (_target.Coords - Controller.Position).ToHorizontal2();
            }
        }
    }
}