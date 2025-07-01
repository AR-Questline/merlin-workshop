using Awaken.TG.Main.AI.Movement.Controllers;

namespace Awaken.TG.Main.AI.States.ReturnToSpawn {
    public class StateReturnAfterVictory : StateReturn {
        protected override StateReturnToSpawnPoint ReturnToSpawnPoint { get; } = new(VelocityScheme.Walk);
        protected override bool RunToSpawn => false;
    }
}
