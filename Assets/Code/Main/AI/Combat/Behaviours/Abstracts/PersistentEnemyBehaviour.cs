namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    /// <summary>
    /// Behaviour that should always be added to Enemy, It won't be removed when changing FighterType.
    /// </summary>
    public abstract partial class PersistentEnemyBehaviour : EnemyBehaviourBase {
        public override bool DisabledForever => false;
    }
}
