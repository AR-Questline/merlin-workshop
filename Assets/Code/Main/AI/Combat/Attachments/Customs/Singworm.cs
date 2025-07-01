using Awaken.Utility;
using System;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class Singworm : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.Singworm;

        public override bool CanMove => CurrentBehaviour.Get() is DigInBehaviour or AttackBehaviour;
    }
}