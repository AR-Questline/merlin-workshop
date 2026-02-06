using Awaken.TG.Main.AudioSystem;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Geysers {
    public class GeyserDataMarker : MonoBehaviour {
        public Transform top;
        public VisualEffect vfx;
        public ARFmodEventEmitter groundEmitterIdle;
        public ARFmodEventEmitter groundEmitterActive;
        public ARFmodEventEmitter topEmitterActive;
        public ARFmodEventEmitter insideEmitterActive;
    }
}