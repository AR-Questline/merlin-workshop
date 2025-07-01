using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.Combat {
    public class VCCharacterMagicVFXMainEffect : VCCharacterMagicVFX {
        protected override void OnCastingSuccessfullyBegun() {
            gameObject.SetActive(true);
        }

        protected override void OnCastingCanceled() {
            AsyncOnCastingCanceled().Forget();
        }

        protected async UniTaskVoid AsyncOnCastingCanceled() {
            if (await AsyncUtil.DelayTime(gameObject, disableVFXDelay)) {
                gameObject.SetActive(false);
            }
        }

        protected override void OnCastingEnded() {
            AsyncOnCastingEnded().Forget();
        }

        protected async UniTaskVoid AsyncOnCastingEnded() {
            if (await AsyncUtil.DelayTime(gameObject, disableVFXDelay)) {
                gameObject.SetActive(false);
            }
        }
    }
}