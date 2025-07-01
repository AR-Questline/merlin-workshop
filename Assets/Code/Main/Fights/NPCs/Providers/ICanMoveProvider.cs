namespace Awaken.TG.Main.Fights.NPCs.Providers {
    public interface ICanMoveProvider {
        bool CanMove { get; }
        bool CanOverrideDestination => true;
        bool ResetMovementSpeed => false;
    }
}