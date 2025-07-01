using Awaken.TG.Main.Stories;
using Awaken.TG.Main.TimeLines.Markers;
using UnityEngine;
using UnityEngine.Playables;

namespace Awaken.TG.Main.TimeLines {
    public class StoryFlagReceiver : MonoBehaviour, INotificationReceiver {
        public void OnNotify(Playable origin, INotification notification, object context) {
            if (notification is StoryFlagMarker marker) {
                StoryFlags.Set(marker.Flag, marker.Value);
            }
        }
    }
}