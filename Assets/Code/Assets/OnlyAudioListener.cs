using System.Linq;
using UnityEngine;

namespace Awaken.TG.Assets {
    public class OnlyAudioListener : MonoBehaviour {
        void Start() {
            var mineListener = GetComponent<AudioListener>();
            if (mineListener == null) {
                Destroy(this);
                return;
            }
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Where(a => a.enabled && a.gameObject.activeInHierarchy);
            if (listeners.Any(l => l != mineListener)) {
                mineListener.enabled = false;
            }
            Destroy(this);
        }
    }
}