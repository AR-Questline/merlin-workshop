using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Character;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    public partial class Mermaid : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.Mermaid;

        public override bool CanLoseTargetBasedOnVisibility => false;
        public override bool CanMove => NpcAI is { InCombat: true } ? false : base.CanMove;
        
        protected override void AfterVisualLoaded(Transform parentTransform) {
            base.AfterVisualLoaded(parentTransform);
            EquipWeapons(false, out _);
            if (!ParentModel.HasElement<HideEnemyFromPlayer>()) {
                ParentModel.AddElement(new HideEnemyFromPlayer(true));
            }
        }
    }
}
