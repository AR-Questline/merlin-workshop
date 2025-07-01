using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Graphics {
    public class ReenableChildrenOnStart : MonoBehaviour {
        public bool onlySelf;
        public WaitMode mode = WaitMode.Frames;
        [FormerlySerializedAs("framesWait")] 
        public int wait = 1;

        public enum WaitMode {
            [UnityEngine.Scripting.Preserve] Frames = 0,
            [UnityEngine.Scripting.Preserve] Milliseconds = 1,
        }
        
        void Start() {
            SetActiveToChildren(false);
            WaitAndActivate().Forget();
        }

        async UniTaskVoid WaitAndActivate() {
            bool success = false;
            if (mode == WaitMode.Frames) {
                success = await AsyncUtil.DelayFrame(gameObject, wait);
            } else {
                success = await AsyncUtil.DelayTime(gameObject, wait);
            }

            if (!success) {
                return;
            }

            SetActiveToChildren(true);
            Destroy(this);
        }

        void SetActiveToChildren(bool active) {
            if (onlySelf) {
                gameObject.SetActive(active);
            } else {
                foreach (Transform child in transform) {
                    child.gameObject.SetActive(active);
                }
            }
        }
    }
}