using System.Collections.Generic;
using CrazyMinnow.SALSA;
using UnityEngine;

namespace Awaken.VendorWrappers.Salsa {
    public class Emoter : MonoBehaviour
    {
        public string emoterName = nameof (Emoter);
        public QueueProcessor queueProcessor;
        [Range(0.0f, 1f)]
        public float lipsyncEmphasisChance = 1f;
        public List<EmoteExpression> lipsyncEmphasisEmotes = new List<EmoteExpression>();
        public List<EmoteRepeater> repeaterEmotes = new List<EmoteRepeater>();
        public List<EmoteExpression> randomEmotes = new List<EmoteExpression>();
        public List<EmoteExpression> emotes = new List<EmoteExpression>();
        public bool useRandomEmotes = true;
        public bool isChancePerEmote = true;
        [SerializeField] private int numRandomEmotesPerCycle;
        [SerializeField] private int numRandomEmphasizersPerCycle;
        public float randomEmoteMinTimer = 1f;
        public float randomEmoteMaxTimer = 2f;
        private float randomPoolTimeCheck;
        private float randomPoolTimeDelay;
        [Range(0.0f, 1f)]
        public float randomChance = 0.5f;
        public bool useRandomFrac;
        [Range(0.0f, 1f)]
        public float randomFracBias = 0.5f;
        public bool useRandomHoldDuration;
        public float randomHoldDurationMin = 0.1f;
        public float randomHoldDurationMax = 0.5f;
        public bool configReady = true;
        public bool warnOnNullRefs = true;
        public bool inspSalsaLinked;
        public bool inspStateFoldoutProcessing;
        public bool inspStateFoldoutSettings;
        public bool inspStateFoldoutEmotes;
        public bool inspCollectionDisplayMode;
    }
}
