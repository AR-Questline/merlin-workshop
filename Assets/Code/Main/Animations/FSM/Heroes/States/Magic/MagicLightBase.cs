using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public abstract partial class MagicLightBase : HeroAnimatorState<MagicFSM>, IMagicLightCast {
        const float DefaultNextCastPerformDelay = 0.25f;

        bool _canPerformNextCast;
        float? _nextCastPerformDelay;
        
        public override bool CanPerformNewAction => _canPerformNextCast;
        public bool CanBeRefunded => !_canPerformNextCast;
        public override bool UsesActiveLayerMask => true;

        // === Initialization
        protected override void OnInitialize() {
            Hero.ListenTo(ICharacter.Events.OnEffectInvokedAnimationEvent, OnPerformCast, this);
        }
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _canPerformNextCast = false;
            _nextCastPerformDelay = null;
            
            ParentModel.ResetAttackProlong();
            ParentModel.ResetBlockProlong();
            
            ParentModel.PlayAudioClip(ItemAudioType.CastBegun.RetrieveFrom(ParentModel.Item));
            Hero.VHeroController?.CastingBegun(ParentModel.CastingHand);
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData {effects = GameConstants.Get.magicLightXboxVibrations, handsAffected = ParentModel.CastingHand});
        }

        protected override void OnUpdate(float deltaTime) {
            if (_nextCastPerformDelay is > 0) {
                _nextCastPerformDelay -= deltaTime;
                if (_nextCastPerformDelay <= 0) {
                    _canPerformNextCast = true;
                    _nextCastPerformDelay = null;
                }
            }
        }
        
        void OnPerformCast(ARAnimationEventData eventData) {
            if (ParentModel.CurrentAnimatorState != this) {
                return;
            }

            if (!eventData.restriction.Match(ParentModel.CastingHand)) {
                return;
            }

            _nextCastPerformDelay = eventData.overrideCastingPerformDelay
                ? eventData.delayNextCastForSeconds
                : DefaultNextCastPerformDelay;
            
            ParentModel.PlayAudioClip(ItemAudioType.CastRelease.RetrieveFrom(ParentModel.Item));
            ParentModel.OnPerformCast();
        }

        protected override void OnExit(bool restarted) {
            _nextCastPerformDelay = null;
            ParentModel.Item?.CancelPerforming(ItemActionType.CastSpell);
            base.OnExit(restarted);
        }
    }
}