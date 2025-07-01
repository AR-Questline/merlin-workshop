using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.Utility;

namespace Awaken.TG.Main.AI.Combat.Attachments.Humanoids {
    public partial class HumanoidCombat : HumanoidCombatBaseClass<HumanoidCombatAttachment> {
        public override ushort TypeForSerialization => SavedModels.HumanoidCombat;

        public override bool UsesCombatMovementAnimations => _usesCombatMovementAnimations;
        public override bool UsesAlertMovementAnimations => _usesAlertMovementAnimations;
        public bool CanLookAround { get; private set; }
        protected override bool CanBePushed => _canBePushed;
        protected override ProjectileData? ArrowOverride => _arrowOverride;
        
        bool _usesCombatMovementAnimations;
        bool _usesAlertMovementAnimations;
        bool _canBePushed;
        ProjectileData _arrowOverride;
        
        // === Initialization
        public override void InitFromAttachment(HumanoidCombatAttachment spec, bool isRestored) {
            MeleeRangedSwitchDistance = spec.meleeRangedSwitchDistance;
            _usesCombatMovementAnimations = spec.usesCombatMovementAnimations;
            _usesAlertMovementAnimations = spec.usesAlertMovementAnimations;
            WeaponsAlwaysEquippedBase = spec.weaponsAlwaysEquipped;
            _canBePushed = spec.canBePushed;
            CanLookAround = spec.canLookAround;
            canBeSlidInto = spec.canBeSlidInto;
            _arrowOverride = spec.customArrowData.ToProjectileData();
        }
    }
}
