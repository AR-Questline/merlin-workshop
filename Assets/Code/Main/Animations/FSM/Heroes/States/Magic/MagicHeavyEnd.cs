using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public partial class MagicHeavyEnd : HeroAnimatorState<MagicFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.MagicCastHeavy;
        public override HeroStateType Type =>
            MagicEndState switch {
                MagicEndState.MagicEnd => HeroStateType.MagicHeavyEnd,
                MagicEndState.MagicEndAlternate1 => HeroStateType.MagicHeavyEndAlternate1,
                MagicEndState.MagicEndAlternate2 => HeroStateType.MagicHeavyEndAlternate2,
                MagicEndState.MagicEndAlternate3 => HeroStateType.MagicHeavyEndAlternate3,
                _ => throw new System.NotImplementedException()
            };
        
        public override bool CanPerformNewAction => TimeElapsedNormalized >= 1;
        public override bool UsesActiveLayerMask => true;
        public bool Performed { get; private set; }
        MagicEndState MagicEndState => ParentModel?.MagicEndState ?? MagicEndState.MagicEnd;
        
        protected override void OnInitialize() {
            Hero.ListenTo(ICharacter.Events.OnEffectInvokedAnimationEvent, OnPerformCast, this);
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            Performed = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
            }
        }

        protected override void OnExit(bool restarted) {
            Performed = false;
            ParentModel.ResetMagicEndIndex();
            ParentModel.Item?.CancelPerforming(ItemActionType.CastSpell);
        }
        
        void OnPerformCast(ARAnimationEventData eventData) {
            if (ParentModel.CurrentAnimatorState != this) {
                return;
            }

            if (!eventData.restriction.Match(ParentModel.CastingHand)) {
                return;
            }
            
            Performed = true;
            
            ParentModel.PlayAudioClip(ItemAudioType.CastHeavyRelease.RetrieveFrom(ParentModel.Item));
            ParentModel.OnPerformCast();
        }
    }

    public enum MagicEndState : byte {
        MagicEnd,
        MagicEndAlternate1,
        MagicEndAlternate2,
        MagicEndAlternate3,
    } 
}