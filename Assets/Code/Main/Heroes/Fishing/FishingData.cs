using System;
using Awaken.TG.Main.General;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Fishing {
    [Serializable]
    public struct FishingData {
        public FloatRange fishingTimeBase;
        public FloatRange fishingTimeLimit;
        [Space] 
        public FloatRange fishingChanceLimit;
        
        [Space]
        public FishTable genericFishTable;

        public float genericFishDensity;
    }
}