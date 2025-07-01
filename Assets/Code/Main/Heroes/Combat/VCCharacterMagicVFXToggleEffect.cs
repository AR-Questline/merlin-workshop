using System.Threading;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public abstract class VCCharacterMagicVFXToggleEffect : VCCharacterMagicVFX {
        [SerializeField] protected bool stopVFXOnDisable;
        
        CancellationTokenSource _tokenSource;
        
        protected async UniTaskVoid ChangeState(bool visible) {
            _tokenSource?.Cancel();
            _tokenSource = new CancellationTokenSource();

            if (!visible && stopVFXOnDisable) {
                VFXUtils.StopVfx(gameObject);
            }
            if (await AsyncUtil.DelayTime(gameObject, visible ? enableVFXDelay : disableVFXDelay, _tokenSource.Token)) {
                gameObject.SetActive(visible);
            }
        }
    }
}