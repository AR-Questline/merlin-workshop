using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Awaken.TG.Main.TimeLines.Markers {
    [CustomStyle("Swap")]
    public class TransitionMarker: Marker, INotification {
        [UnityEngine.Scripting.Preserve] public bool endCutscene;
        public PropertyName id => GetHashCode();
    }
}