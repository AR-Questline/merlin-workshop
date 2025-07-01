using Awaken.Utility;
using System;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class AlwaysPiercedEnemy : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.AlwaysPiercedEnemy;

        protected override void OnInitialize() {
            base.OnInitialize();
            NpcElement.IsAlwaysPiercedByArrows = true;
        }
    }
}