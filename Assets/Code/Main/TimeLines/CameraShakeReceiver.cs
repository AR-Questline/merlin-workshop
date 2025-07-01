using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.TimeLines.Markers;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace Awaken.TG.Main.TimeLines {
    public class CameraShakeReceiver : MonoBehaviour, INotificationReceiver {
        public void OnNotify(Playable origin, INotification notification, object context) {
            if (notification is CameraShakeMarker csm) {
                World.Only<GameCamera>().Shake(false, csm.shakeAmplitude, csm.shakeFrequency, csm.shakeTime, csm.shakePick).Forget();
            }
        }
    }
}