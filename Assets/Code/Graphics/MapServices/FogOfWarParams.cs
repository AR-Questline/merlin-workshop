using System;
using UnityEngine;

namespace Awaken.TG.Graphics.MapServices {
    [Serializable]
    public struct FogOfWarParams {
        [Min(1)] public int revealPixelsRadius;
        [Range(0, 1), Tooltip("Percent of the radius which, when exceeded, triggers revealing area on current position")] 
        public float revealRadiusThreshold;
        [Range(0, 2)] public float breakDistanceInCirclesBetween;
        [Range(0, 1)] public float maskTextureMultiplier;
        [Range(0, 1)] public float revealBrushIntensity;
        public int softAreaPixelsCount;
        public int falloffPow;
    }
}