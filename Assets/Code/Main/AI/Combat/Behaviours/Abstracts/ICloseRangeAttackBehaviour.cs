namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    /// <summary>
    /// Marker interface so that other behaviours can trigger CloseRangeAttack without worrying which one is implemented by their parent.
    /// </summary>
    public interface ICloseRangeAttackBehaviour : IBehaviourBase {
        float MaxDistance { get; }
        bool InRange { get; }
    }
}