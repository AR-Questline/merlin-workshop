using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Audio;
using Awaken.Utility.Debugging;
using FMODUnity;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Shared {
    public abstract partial class UnEquipWeaponBase<T> : HeroAnimatorState<T> where T : HeroAnimatorSubstateMachine {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.UnEquip;
        public override HeroStateType Type => HeroStateType.UnEquipWeapon;
        public override HeroStateType StateToEnter => UseAlternateState ? HeroStateType.UnEquipWeaponAlternate : HeroStateType.UnEquipWeapon;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            PlaySheatheAudio();
        }

        protected override void OnUpdate(float deltaTime) {
            if (ParentModel.EquipInputPressed && Hero.CanUseEquippedWeapons) {
                ParentModel.SetCurrentState(HeroStateType.EquipWeapon, 0.25f);
                return;
            }
            
            if (TimeElapsedNormalized is >= 1 or <= -1) {
                ParentModel.SetCurrentState(HeroStateType.Empty);
            }
        }

        protected override void OnExit(bool restarted) {
            ParentModel.ResetInput();
        }
        
        void PlaySheatheAudio() {
            Item weapon = ParentModel.MainHandItem;
            if(weapon == null) {
                Log.Important?.Error("Failed to play Sheathe audio because MainHandItem for Hero is null");
                return;
            }

            EventReference eventReference = ItemAudioType.Sheathe.RetrieveFrom(weapon);
            if(!eventReference.IsNull) {
                FMODManager.PlayAttachedOneShotWithParameters(eventReference, Hero.ParentTransform.gameObject, Hero.ParentTransform);
            }
        }
    }
}