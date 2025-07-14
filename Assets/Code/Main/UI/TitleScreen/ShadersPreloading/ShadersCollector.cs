using System.Threading;
using Awaken.TG.Assets.ShadersPreloading;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.UI;
using Cysharp.Threading.Tasks;
using QFSW.QC;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.VFX;

namespace Awaken.TG.Main.UI.TitleScreen.ShadersPreloading {
    public static class ShadersCollector {
        static SaveBlocker s_saveBlocker;
        static CancellationTokenSource s_cancellationTokenSource;

        static int s_allTestCount;
        static int s_currentTestIndex;

        [Command("shadersTracer.startCollecting", "")]
        static void StartCollecting() {
            CollectingRoutine().Forget();
        }

        static async UniTaskVoid CollectingRoutine() {
            QuantumConsole.Instance.Deactivate();

            var vfxCollection = Addressables.LoadAssetAsync<VfxCollection>("VfxCollection").WaitForCompletion();

            CollectorWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
            s_cancellationTokenSource?.Cancel();
            s_cancellationTokenSource = new CancellationTokenSource();

            var token = s_cancellationTokenSource.Token;

            ShadersTracer.StartTracingCommand();

            PrepareEnvironment();

            var vfxPrefabsCount = vfxCollection.vfxPrefabs.Length;
            var vfxAssetsCount = vfxCollection.vfxAssets.Length;
            s_allTestCount = vfxPrefabsCount + vfxAssetsCount;

            var position = Hero.Current.Rotation * Vector3.forward * 12f;

            for (int i = 0; i < vfxPrefabsCount; i++) {
                if (token.IsCancellationRequested) {
                    break;
                }

                ++s_currentTestIndex;
                var prefab = vfxCollection.vfxPrefabs[i];
                var vfx = Object.Instantiate(prefab, position, Quaternion.identity);
                EnableAllGameObjects(vfx);
                await UniTask.DelayFrame(60);
                Object.Destroy(vfx);
            }

            await UniTask.NextFrame();

            var host = new GameObject("VFX_Host", typeof(VisualEffect));
            host.transform.position = position;
            var effect = host.GetComponent<VisualEffect>();

            for (int i = 0; i < vfxAssetsCount; i++) {
                if (token.IsCancellationRequested) {
                    break;
                }

                ++s_currentTestIndex;
                var vfxAsset = vfxCollection.vfxAssets[i];
                host.SetActive(false);
                effect.visualEffectAsset = vfxAsset;
                await UniTask.NextFrame();
                host.SetActive(true);
                await UniTask.DelayFrame(60);
            }

            Object.Destroy(host);

            await UniTask.NextFrame();
            ShadersTracer.SaveTracingResultsToFileCommand();

            CleanupEnvironment();
            CollectorWindow.Close();
        }

        static void PrepareEnvironment() {
            s_saveBlocker = World.Add(new SaveBlocker("ShadersCollector"));

            World.Only<UIStateStack>().PushState(UIState.Cursor, s_saveBlocker);
        }

        static void CleanupEnvironment() {
            s_saveBlocker?.Discard();
            s_saveBlocker = null;
        }

        static void EnableAllGameObjects(GameObject vfx) {
            var allTransforms = vfx.GetComponentsInChildren<Transform>(true);
            foreach (var tr in allTransforms) {
                tr.gameObject.SetActive(true);
            }
        }

        class CollectorWindow : UGUIWindowDisplay<CollectorWindow> {
            protected override void DrawWindow() {
                var progress = (float)s_currentTestIndex / s_allTestCount;
                GUILayout.Label($"Progress: {progress:P1} [{s_currentTestIndex}/{s_allTestCount}]");
                GUILayout.HorizontalSlider(progress, 0f, 1f);
            }

            protected override void Shutdown() {
                s_cancellationTokenSource?.Cancel();
            }
        }
    }
}
