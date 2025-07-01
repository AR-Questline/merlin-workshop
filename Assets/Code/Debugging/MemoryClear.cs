using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using QFSW.QC;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Debugging {
    public class MemoryClear : MonoBehaviour {
        void Awake() {
            DontDestroyOnLoad(gameObject);
        }

        [Button]
        void Clear() {
            ClearAll();
        }

        [Command("clr-mem-all", "Clears all memory")][UnityEngine.Scripting.Preserve]
        public static void ClearAll() {
            DOTween.Clear();
            DOTween.ClearCachedTweens();
            ReferencesCachesRevalidate();
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public static async UniTask ClearProgramming() {
            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public static void ReferencesCachesRevalidate() {
            GraphReference.FreeInvalidInterns();
            World.Any<FocusHistorian>()?.ClearInvalidEntries();
        }
    }
}