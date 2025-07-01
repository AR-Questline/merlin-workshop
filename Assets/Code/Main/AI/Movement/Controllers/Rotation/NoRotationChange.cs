namespace Awaken.TG.Main.AI.Movement.Controllers.Rotation {
    public class NoRotationChange : IRotationScheme {
        public NpcController Controller { get; set; }

        public void Enter() {
            Controller.SetRotationInstant(Controller.Rotation);
        }
        public void Update(float deltaTime) { }
        public bool UseRichAIRotation => false;
    }
}