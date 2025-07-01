using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Audio;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using FMODUnity;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Shared {
    public abstract partial class EquipWeaponBase<T> : HeroAnimatorState<T> where T : HeroAnimatorSubstateMachine {
        bool _unsheatheSoundPlayed;

        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.EquipWeapon;
        public override HeroStateType StateToEnter => UseAlternateState ? HeroStateType.EquipWeaponAlternate : HeroStateType.EquipWeapon;
        public override bool CanPerformNewAction => TimeElapsedNormalized >= Hero.Stat(HeroStatType.EquipWeaponActionCooldown).ModifiedValue;
        public override float EntryTransitionDuration => 0f;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.ResetInput();
            _unsheatheSoundPlayed = false;
            UpdateLegsAvatarAfterDelay().Forget();
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
            }

            if (!_unsheatheSoundPlayed) {
                PlayUnsheatheAudio();
            }
        }
        
        void PlayUnsheatheAudio() {
            _unsheatheSoundPlayed = true;
            Item weapon = ParentModel.MainHandItem;
            if(weapon == null) {
                Log.Important?.Error("Failed to play Unsheathe audio because MainHandItem for Hero is null");
                return;
            }
            
            EventReference eventReference = ItemAudioType.Unsheathe.RetrieveFrom(weapon);
            if (!eventReference.IsNull) {
                FMODManager.PlayAttachedOneShotWithParameters(eventReference, Hero.ParentTransform.gameObject, Hero.ParentTransform);
            }
        }

        async UniTaskVoid UpdateLegsAvatarAfterDelay() {
            if (await AsyncUtil.DelayFrame(this)) {
                Hero.TryGetElement<LegsFSM>()?.UpdateAvatarMask();
            }
        }
    }
}