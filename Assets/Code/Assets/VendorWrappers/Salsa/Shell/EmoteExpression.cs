using UnityEngine;

namespace CrazyMinnow.SALSA
{
    public class EmoteExpression
    {
        [SerializeField]
        public Expression expData = new Expression();
        public bool isRandomEmote;
        public bool isLipsyncEmphasisEmote;
        public bool isRepeaterEmote;
        public float repeaterDelay;
        public EmoteRepeater.StartDelay startDelay;
        public bool isPersistent;
        public bool isHoldVariationExempt;
        public bool isAlwaysEmphasisEmote;
        public bool hasBeenFired;
        public float frac = 1f;
    }
}