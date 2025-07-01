namespace Awaken.TG.Main.Heroes.Combat {
    public class VCCharacterMagicVFXChargeEffect : VCCharacterMagicVFXToggleEffect {
        protected override void Initialize() {
            base.Initialize();
            gameObject.SetActive(false);
        }

        protected override void OnCastingBegun() {
            ChangeState(true).Forget();
        }

        protected override void OnCastingCanceled() {
            ChangeState(false).Forget();
        }
        
        protected override void OnCastingEnded() {
            ChangeState(false).Forget();
        }
    }
}