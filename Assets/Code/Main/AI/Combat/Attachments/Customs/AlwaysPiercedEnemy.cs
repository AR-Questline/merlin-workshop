using Awaken.Utility;
using System;
using Awaken.TG.Main.Fights.NPCs;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class AlwaysPiercedEnemy : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.AlwaysPiercedEnemy;

        protected override void OnInitializeInternal() {
            base.OnInitializeInternal();
            NpcElement.IsAlwaysPiercedByArrows = true;
        }
    }
}