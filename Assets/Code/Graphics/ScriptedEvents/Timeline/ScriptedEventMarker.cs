using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Awaken.TG.Graphics.ScriptedEvents.Timeline {
    public class ScriptedEventMarker : Marker, INotification {
        public PropertyName id => GetHashCode();

        [field: SerializeField] public ScriptedEventEventType Type { get; private set; }
    }
}