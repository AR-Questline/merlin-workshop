using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath.Forwarder {
    public partial class FinisherDeathAnimationForwarder : DeathBehaviourUpdater {
        readonly PostponedRagdollBehaviourBase.RagdollEnableData _data;

        public sealed override bool IsNotSaved => true;
        
        public FinisherDeathAnimationForwarder(PostponedRagdollBehaviourBase.RagdollEnableData data) {
            _data = data;
        }
        
        protected override void UpdateDeathBehaviours(GameObject visualGO, CustomDeathController customDeathController) {
            customDeathController.SetRagdollOnDeath(true, null, false);
            if (!visualGO.HasComponent<FinisherDeathAnimations>()) {
                var deathAnimations = visualGO.AddComponent<FinisherDeathAnimations>();
                deathAnimations.Setup(_data);
            }
        }
    }
}
