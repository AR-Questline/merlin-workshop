using System;
using Awaken.TG.Main.AudioSystem.Biomes;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Fishing {
    [Serializable]
    public struct FishingAudio {
        [SerializeReference] public IAudioSource[] music;
        [SerializeReference] public IAudioSource[] ambient;
        [SerializeReference] public IAudioSource[] snapshots;
        
        [Space(5)]
        public EventReference rodCastingStart;
        public EventReference rodCastingThrow;
        public EventReference rodCatch;
        public EventReference fishingRodStruggling;
        
        [Space(5)]
        public EventReference bobberHitWater;
        public EventReference bobberSubmergeWithCatch;
        public EventReference bobberSubmergeFakeCatch;

        [Space(5)]
        public EventReference catchGarbage;
        public EventReference catchCommonFish;
        public EventReference catchUncommonFish;
        public EventReference catchRareFish;
        public EventReference catchLegendaryFish;
        
        [Space(5)]
        public EventReference lineBreak;
        public EventReference fishFighting;
    }
}