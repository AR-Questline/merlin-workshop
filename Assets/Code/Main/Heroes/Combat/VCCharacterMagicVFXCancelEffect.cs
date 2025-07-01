using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.Combat {
    public class VCCharacterMagicVFXCancelEffect : VCCharacterMagicVFX {
        CancellationTokenSource _token;
        
        protected override void Initialize() {
            base.Initialize();
            gameObject.SetActive(false);
        }

        protected override void OnCastingCanceled() {
            AsyncOnCastingCanceled().Forget();
        }

        protected async UniTaskVoid AsyncOnCastingCanceled() {
            _token?.Cancel();
            _token = new CancellationTokenSource();
            gameObject.SetActive(true);
            if (await AsyncUtil.DelayTime(gameObject, disableVFXDelay, source: _token)) {
                gameObject.SetActive(false);
            }
        }

        protected override void OnCastingBegun() {
            _token?.Cancel();
            _token = new CancellationTokenSource();
            gameObject.SetActive(false);
        }
    }
}