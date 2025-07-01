namespace Awaken.TG.Main.Heroes.Combat {
    public class VCCharacterMagicAlwaysActiveEffect : VCCharacterMagicVFX {
        protected override void Initialize() {
            base.Initialize();
            gameObject.SetActive(true);
        }
    }
}