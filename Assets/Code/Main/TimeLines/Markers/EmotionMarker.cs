using System;
using CrazyMinnow.SALSA;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Awaken.TG.Main.TimeLines.Markers {
    public class EmotionMarker : Marker, INotification, INotificationOptionProvider {
        public PropertyName id => GetHashCode();

        public string emotionKey;
        public EmotionState emotionState;
        public float duration;
        public ExpressionComponent.ExpressionHandler expressionHandler;
        public NotificationFlags flags => NotificationFlags.TriggerInEditMode;
    }

    public enum EmotionState {
        [UnityEngine.Scripting.Preserve] Enable = 0,
        [UnityEngine.Scripting.Preserve] Disable = 1
    }

    [Serializable]
    public struct EmotionData {
        public double startTime;
        public string emotionKey;
        public EmotionState state;
        public ExpressionComponent.ExpressionHandler expressionHandler;
        public float roundDuration;

        public EmotionData(double startTime, float roundDuration, ExpressionComponent.ExpressionHandler expressionHandler, string emotionKey, EmotionState emotionState) {
            this.startTime = startTime;
            this.emotionKey = emotionKey;
            state = emotionState;
            this.roundDuration = roundDuration;
            this.expressionHandler = expressionHandler;
        }
    }
}
