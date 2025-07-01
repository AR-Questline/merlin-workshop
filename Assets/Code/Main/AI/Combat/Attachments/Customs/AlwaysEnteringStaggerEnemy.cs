using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.Utility;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [UnityEngine.Scripting.Preserve]
    public partial class AlwaysEnteringStaggerEnemy : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.AlwaysEnteringStaggerEnemy;

        public override void EnterParriedState() {
            if (Staggered) {
                return;
            }
            
            _shouldEnterStaggerOrRest = true;
        }

        protected override void OnBehaviourStopped(IBehaviourBase exitedBehaviour) {
            TryEnterStagger(exitedBehaviour);
        }
        
        protected override void OnBehaviourInterrupted(IBehaviourBase exitedBehaviour) {
            TryEnterStagger(exitedBehaviour);
        }

        void TryEnterStagger(IBehaviourBase exitedBehaviour) {
            if (Staggered) {
                return;
            }
            
            if (exitedBehaviour is CombatEnemyBehaviourBase) {
                _shouldEnterStaggerOrRest = true;
            }
        }
    }
}