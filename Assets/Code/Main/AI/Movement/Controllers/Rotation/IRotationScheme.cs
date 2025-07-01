namespace Awaken.TG.Main.AI.Movement.Controllers.Rotation {
    public interface IRotationScheme {
        NpcController Controller { get; set; }
        bool UseRichAIRotation { get; }
        void Enter();
        void Update(float deltaTime);
    }
}