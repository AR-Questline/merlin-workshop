using UnityEngine;

namespace Awaken.VendorWrappers.Salsa {
    public class SalsaAdvancedDynamicsSilenceAnalyzer : MonoBehaviour {
        public Salsa salsaInstance;
        public int bufferSize = 512;
        private float[] samples;
        [Range(0.0f, 1f)]
        public float silenceThreshold = 0.6f;
        [Range(0.0f, 1f)]
        public float timingStartPoint = 0.4f;
        [Range(0.0f, 1f)]
        public float timingEndVariance = 0.97f;
        [Range(0.0f, 1f)]
        public float silenceSampleWeight = 0.95f;
        public int silenceHits;
    }
}