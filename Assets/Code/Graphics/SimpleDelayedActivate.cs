using System;
using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG {
    public class SimpleDelayedActivate : MonoBehaviour {
        [SerializeField] GameObject[] gameObjectsToActivate = Array.Empty<GameObject>();
        [SerializeField] FloatRange delayRandom = new(0.1f, 2f);
        CancellationTokenSource _cancellationTokenSource;

        void OnEnable() {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            foreach (var go in gameObjectsToActivate) {
                if (go == null) {
                    continue;
                }
                
                ActivateGameObjectWithDelay(go, delayRandom.RandomPick(), _cancellationTokenSource.Token).Forget();
            }
        }

        void OnDisable() {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            
            foreach (var go in gameObjectsToActivate) {
                if (go == null) {
                    continue;
                }
                
                go.SetActive(false);
            }
        }

        static async UniTaskVoid ActivateGameObjectWithDelay(GameObject go, float delay, CancellationToken cancelToken) {
            if (!await AsyncUtil.DelayTime(go, delay, cancelToken)) {
                return;
            }

            go.SetActive(true);
        }
    }
}
