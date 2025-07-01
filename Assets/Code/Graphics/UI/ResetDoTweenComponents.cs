using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;

namespace Awaken.TG.Graphics.UI {
    public class ResetDoTweenComponents : MonoBehaviour {
        void Start() {
            ResetAfter().Forget();
        }

        async UniTaskVoid ResetAfter() {
            await UniTask.DelayFrame(5);
            foreach (var d in FindObjectsByType<DOTweenComponent>(FindObjectsSortMode.None)) {
                d.DORestart();
            }
        }
    }
}