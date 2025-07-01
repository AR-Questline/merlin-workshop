using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC.Elements;
using Unity.Mathematics;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Utils {
    public partial class AnimatorSharedData : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        public bool MagicHeld { get; private set; }
        Stat MovementSpeedStat => ParentModel.CharacterStats.MovementSpeedMultiplier;
        Stat AimSensitivityMultiplier => ParentModel.HeroStats.AimSensitivityMultiplier;
        HeroControllerData Data => ParentModel.Data;
        StatTweak _magicSlowSpeedModifier, _aimSensitivityMultiplierModifier;
        float _desiredMagicHeldSpeedMultiplier;
        float _magicHeldSpeedMultiplier;
        
        public void BeginMagicSlowModifier() {
            MagicHeld = true;

            _desiredMagicHeldSpeedMultiplier = GetDesiredMagicHeldSpeedMultiplier(ParentModel.Elements<MagicFSM>());
            
            if (_magicSlowSpeedModifier == null || _magicSlowSpeedModifier.HasBeenDiscarded) {
                _magicHeldSpeedMultiplier = 1;
                _magicSlowSpeedModifier = StatTweak.Multi(MovementSpeedStat, _magicHeldSpeedMultiplier, TweakPriority.Multiply, this);
                ParentModel.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate);
            }
            
            if (_aimSensitivityMultiplierModifier == null || _aimSensitivityMultiplierModifier.HasBeenDiscarded) {
                _aimSensitivityMultiplierModifier = StatTweak.Multi(AimSensitivityMultiplier, Data.aimSensitivityMultiplier, TweakPriority.Multiply, this);
            }
        }

        void ProcessUpdate(float deltaTime) {
            if (MagicHeld) {
                if (math.abs(_magicHeldSpeedMultiplier - _desiredMagicHeldSpeedMultiplier) <= 0.001f) {
                    return;
                }

                if (_magicHeldSpeedMultiplier > _desiredMagicHeldSpeedMultiplier) {
                    _magicHeldSpeedMultiplier -= deltaTime;
                    _magicHeldSpeedMultiplier = math.clamp(_magicHeldSpeedMultiplier, _desiredMagicHeldSpeedMultiplier, 1);
                } else {
                    _magicHeldSpeedMultiplier += deltaTime;
                    _magicHeldSpeedMultiplier = math.clamp(_magicHeldSpeedMultiplier, 0, _desiredMagicHeldSpeedMultiplier);
                }
                
                _magicSlowSpeedModifier.SetModifier(_magicHeldSpeedMultiplier);
                return;
            }

            if (_magicHeldSpeedMultiplier < 1) {
                _magicHeldSpeedMultiplier += deltaTime;
                if (_magicHeldSpeedMultiplier >= 1) {
                    MagicHeldEnded();
                    return;
                }
                _magicSlowSpeedModifier.SetModifier(_magicHeldSpeedMultiplier);
            }
        }

        public void EndMagicSlowModifier() {
            var magicFsms = ParentModel.Elements<MagicFSM>();
            if (magicFsms.Any(m => m.IsChargingMagic)) {
                _desiredMagicHeldSpeedMultiplier = GetDesiredMagicHeldSpeedMultiplier(magicFsms);
                return;
            }
            
            MagicHeld = false;
            _aimSensitivityMultiplierModifier?.Discard();
            _aimSensitivityMultiplierModifier = null;
        }

        void MagicHeldEnded() {
            _magicSlowSpeedModifier?.Discard();
            _magicSlowSpeedModifier = null;
            ParentModel.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
        }
        
        // === Helpers
        static float GetDesiredMagicHeldSpeedMultiplier(ModelsSet<MagicFSM> magicFsms) {
            var minMultiplier = float.MaxValue;
            foreach (var magicFsm in magicFsms) {
                if (magicFsm.IsChargingMagic) {
                    minMultiplier = math.min(minMultiplier, magicFsm.Item.ItemStats.MagicHeldSpeedMultiplier.ModifiedValue);
                }
            }
            return minMultiplier;
        }
    }
}