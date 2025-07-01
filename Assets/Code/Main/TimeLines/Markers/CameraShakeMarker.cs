using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Awaken.TG.Main.TimeLines.Markers {
    public class CameraShakeMarker : Marker, INotification {
        public PropertyName id => GetHashCode();
        
        public float shakeAmplitude = 0.5f;
        public float shakeFrequency = 0.15f;
        public float shakeTime = 0.5f;
        public float shakePick = 0.1f;
    }
}