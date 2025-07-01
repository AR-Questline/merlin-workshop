using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    /// <summary>
    /// Marker for forwarded enemy behaviour, that can't be used directly.
    /// Behaviours with this markers can be only started from EnemyBehaviourForwarder.
    /// </summary>
    public partial class EnemyBehaviourForwardedMarker : Element<EnemyBehaviourBase> {
        public sealed override bool IsNotSaved => true;
    }
}