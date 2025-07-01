namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    /// <summary>
    /// Marker interface so that other behaviours can trigger WaitBehaviour without worrying which one is implemented by their parent.
    /// </summary>
    public interface IWaitBehaviour : IBehaviourBase {}
}