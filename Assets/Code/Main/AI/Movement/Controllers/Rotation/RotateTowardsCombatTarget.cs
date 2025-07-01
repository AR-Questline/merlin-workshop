using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.Controllers.Rotation {
    public class RotateTowardsCombatTarget : IRotationScheme {
        public NpcController Controller { get; set; }
        public bool UseRichAIRotation => _target == null;
        ICharacter _target;
        
        public void Enter() { }
        
        public void Update(float deltaTime) {
            _target = Controller.Npc?.GetCurrentTarget();
            if (_target != null) {
                Controller.SteeringDirection = (_target.Coords - Controller.Position).ToHorizontal2();
            }
        }
    }
}