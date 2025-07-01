using Awaken.TG.Utility;

namespace Awaken.TG.Main.AI.Movement.Controllers.Rotation {
    public class RotateTowardsMovement : IRotationScheme {
        public NpcController Controller { get; set; }
        public bool UseRichAIRotation => true;
        public void Enter() { }
        public void Update(float deltaTime) {
            if (!Controller.UseRichAIRotation) {
                Controller.SteeringDirection = (Controller.RichAI.steeringTarget - Controller.Position).ToHorizontal2();
            }
        }
    }
}