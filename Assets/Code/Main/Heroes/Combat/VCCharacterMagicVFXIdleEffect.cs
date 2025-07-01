namespace Awaken.TG.Main.Heroes.Combat {
    public class VCCharacterMagicVFXIdleEffect : VCCharacterMagicVFXToggleEffect {
        protected override void Initialize() {
            base.Initialize();
            gameObject.SetActive(true);
        }

        protected override void OnCastingBegun() {
            ChangeState(false).Forget();
        }

        protected override void OnCastingCanceled() {
            ChangeState(true).Forget();
        }

        protected override void OnCastingEnded() {
            ChangeState(true).Forget();
        }
    }
}