using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Assets {
    /// <summary>
    /// Gating children UI object until all locking elements unlock.
    /// </summary>
    public class AssetLoadingGate : MonoBehaviour, IAssetLoadingGate {
        const int MinimumFrames = 2;
        [Required] public CanvasGroup gate;
        public bool gateOnlyOnCreation = true;
        [SerializeField] WaitType waitType;
        [SerializeField, ShowIf(nameof(waitType), WaitType.Seconds)] float secondsToWait;
        [SerializeField, ShowIf(nameof(waitType), WaitType.Frames)] int framesToWait;

        uint _semaphore;
        uint _frames;
        float _alpha;
        
        public View OwnerView => gameObject.GetComponentInParent<View>();
        public bool IsLocked => _semaphore > 0;

        void OnEnable() {
            if (waitType != WaitType.None) {
                gateOnlyOnCreation = false;
                UnlockWithDelay().Forget();
            }
            
            if (gateOnlyOnCreation) {
                World.Services.Get<UnityUpdateProvider>().RegisterAssetLoadingGate(this);
            }
        }

        void OnDisable() {
            World.Services?.Get<UnityUpdateProvider>().UnregisterAssetLoadingGate(this);
        }

        public bool TryLock() {
            // This prevents flickering when additional elements get spawn after window has been shown 
            if (gateOnlyOnCreation && _frames > MinimumFrames) {
                return false;
            }
            if (_semaphore == 0) {
                _alpha = gate.alpha;
            }
            _semaphore++;
            gate.alpha = 0;
            return true;
        }

        public void Unlock() {
            _semaphore--;
            if (_semaphore == 0 && gate != null) {
                gate.alpha = _alpha;
            }
        }

        public void UnityUpdate() {
            _frames++;
            // Frames are used only in TryLock, so unregister if there won't be any change anymore
            if (_frames > MinimumFrames) {
                World.Services.Get<UnityUpdateProvider>().UnregisterAssetLoadingGate(this);
            }
        }
        
        async UniTaskVoid UnlockWithDelay() {
            TryLock();
            
            if (waitType == WaitType.Frames) {
                await AsyncUtil.DelayFrame(this, framesToWait);
            } else if (waitType == WaitType.Seconds) {
                await AsyncUtil.DelayTime(this, secondsToWait, true);
            }
            
            Unlock();
        }
        
        enum WaitType : byte {
            None,
            Frames,
            Seconds
        }
    }
}